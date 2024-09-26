using FluentAssertions;
using FluentAssertions.Extensions;
using Laerdal.McuMgr.Common.Enums;
using Laerdal.McuMgr.Common.Events;
using Laerdal.McuMgr.FirmwareInstaller.Contracts.Enums;
using Laerdal.McuMgr.FirmwareInstaller.Contracts.Events;
using Laerdal.McuMgr.FirmwareInstaller.Contracts.Native;
using GenericNativeFirmwareInstallerCallbacksProxy_ = Laerdal.McuMgr.FirmwareInstaller.FirmwareInstaller.GenericNativeFirmwareInstallerCallbacksProxy;

#pragma warning disable xUnit1026

namespace Laerdal.McuMgr.Tests.FirmwareInstaller
{
    /// <summary>
    /// Certain exotic devices (like Samsung A8 Android tablets) have buggy ble-stacks and have reliable support only for Phy1M mode and even then they have a
    /// problem with establishing a ble-connection with nRF51+ chipsets of Nordic unless BeginInstallation() / UploadAsync() / DownloadAsync() are called
    /// with initialMtuValue=23 (which is the only value that works for these exotic devices).
    ///
    /// We need to ensure that the retry logic is able to handle such problematic devices by lowering the initialMtuValue to 23 in the last few retries
    /// (as a last ditch best-effort.)
    /// </summary>
    public partial class FirmwareInstallerTestbed
    {
        [Theory]
        [InlineData("FIT.IA.SCSWLDAULIM.GPDFFU.010", 2)]
        [InlineData("FIT.IA.SCSWLDAULIM.GPDFFU.020", 3)]
        [InlineData("FIT.IA.SCSWLDAULIM.GPDFFU.030", 5)]
        public async Task InstallAsync_ShouldCompleteSuccessfullyWithLastDitchAttemptUsingLowerInitialMtu_GivenProblematicDeviceForFileUploading(string testNickname, int maxTriesCount)
        {
            // Arrange
            var mockedNativeFirmwareInstallerProxy = new MockedGreenNativeFirmwareInstallerProxySpy10(new GenericNativeFirmwareInstallerCallbacksProxy_(), maxTriesCount);
            var firmwareInstaller = new McuMgr.FirmwareInstaller.FirmwareInstaller(mockedNativeFirmwareInstallerProxy);

            using var eventsMonitor = firmwareInstaller.Monitor();

            // Act
            var work = new Func<Task>(() => firmwareInstaller.InstallAsync([1, 2, 3], maxTriesCount: maxTriesCount));

            // Assert
            await work.Should().CompleteWithinAsync(4.Seconds());
            
            mockedNativeFirmwareInstallerProxy.BugDetected.Should().BeNull();

            mockedNativeFirmwareInstallerProxy.CancelCalled.Should().BeFalse();
            mockedNativeFirmwareInstallerProxy.DisconnectCalled.Should().BeFalse(); //00
            mockedNativeFirmwareInstallerProxy.BeginInstallationCalled.Should().BeTrue();
            
            eventsMonitor
                .OccurredEvents
                .Count(x => x.EventName == nameof(firmwareInstaller.FatalErrorOccurred))
                .Should()
                .Be(maxTriesCount - 1);
            
            eventsMonitor
                .OccurredEvents //there should be only one completed event
                .Count(x => x.Parameters.OfType<StateChangedEventArgs>().FirstOrDefault() is { NewState: EFirmwareInstallationState.Complete })
                .Should()
                .Be(1);

            eventsMonitor
                .OccurredEvents
                .Count(x =>
                {
                    var logEventArgs = x.Parameters.OfType<LogEmittedEventArgs>().FirstOrDefault(); //                      we need to make sure the calling environment
                    return logEventArgs is { Level: ELogLevel.Warning } && logEventArgs.Message.Contains("[FI.IA.010]"); // is warned about falling back to failsafe settings
                })
                .Should()
                .Be(1);

            eventsMonitor
                .Should().Raise(nameof(firmwareInstaller.StateChanged))
                .WithSender(firmwareInstaller)
                .WithArgs<StateChangedEventArgs>(args => args.NewState == EFirmwareInstallationState.Uploading);
            
            eventsMonitor
                .Should()
                .Raise(nameof(firmwareInstaller.FirmwareUploadProgressPercentageAndDataThroughputChanged))
                .WithSender(firmwareInstaller);
            
            //00 we dont want to disconnect the device regardless of the outcome
        }

        private class MockedGreenNativeFirmwareInstallerProxySpy10 : MockedNativeFirmwareInstallerProxySpy
        {
            private int _tryCounter;
            private readonly int _maxTriesCount;
            
            public string BugDetected { get; private set; }
            
            public MockedGreenNativeFirmwareInstallerProxySpy10(INativeFirmwareInstallerCallbacksProxy firmwareInstallerCallbacksProxy, int maxTriesCount)
                : base(firmwareInstallerCallbacksProxy)
            {
                _maxTriesCount = maxTriesCount;
            }

            public override EFirmwareInstallationVerdict BeginInstallation(
                byte[] data,
                EFirmwareInstallationMode mode = EFirmwareInstallationMode.TestAndConfirm,
                bool? eraseSettings = null,
                int? estimatedSwapTimeInMilliseconds = null,
                int? initialMtuSize = null,
                int? windowCapacity = null,
                int? memoryAlignment = null,
                int? pipelineDepth = null,
                int? byteAlignment = null
            )
            {
                if (BugDetected is not null)
                    throw new Exception(BugDetected);
                
                _tryCounter++;

                var verdict = base.BeginInstallation(
                    data: data,
                    mode: mode,
                    eraseSettings: eraseSettings,
                    pipelineDepth: pipelineDepth,
                    byteAlignment: byteAlignment,
                    initialMtuSize: initialMtuSize,
                    windowCapacity: windowCapacity,
                    memoryAlignment: memoryAlignment,
                    estimatedSwapTimeInMilliseconds: estimatedSwapTimeInMilliseconds
                );

                Task.Run(function: async () => //00 vital
                {
                    if (_tryCounter == 1 && initialMtuSize == 23)
                    {
                        BugDetected = "[BUG DETECTED] The very first try should not be with initialMtuSize set to 23 - something is wrong!";
                        StateChangedAdvertisement(oldState: EFirmwareInstallationState.Uploading, newState: EFirmwareInstallationState.Error);
                        FatalErrorOccurredAdvertisement(EFirmwareInstallationState.Uploading, EFirmwareInstallerFatalErrorType.FirmwareUploadingErroredOut, BugDetected);
                        return;
                    }
                    
                    await Task.Delay(10);
                    
                    StateChangedAdvertisement(oldState: EFirmwareInstallationState.Idle, newState: EFirmwareInstallationState.Idle);
                    await Task.Delay(10);
                    
                    StateChangedAdvertisement(oldState: EFirmwareInstallationState.Idle, newState: EFirmwareInstallationState.Validating);
                    await Task.Delay(10);
                    
                    StateChangedAdvertisement(oldState: EFirmwareInstallationState.Validating, newState: EFirmwareInstallationState.Uploading);
                    await Task.Delay(100);

                    { //file uploading simulation
                        FirmwareUploadProgressPercentageAndDataThroughputChangedAdvertisement(progressPercentage: 00, averageThroughput: 00);
                        await Task.Delay(10);
                        
                        if (_tryCounter == _maxTriesCount && initialMtuSize != 23)
                        {
                            BugDetected = $"[BUG DETECTED] The very last try should be with initialMtuSize set to 23 but it's set to {initialMtuSize} - something is wrong!";
                            StateChangedAdvertisement(oldState: EFirmwareInstallationState.Uploading, newState: EFirmwareInstallationState.Error);
                            FatalErrorOccurredAdvertisement(EFirmwareInstallationState.Uploading, EFirmwareInstallerFatalErrorType.FirmwareUploadingErroredOut, BugDetected);
                            return;
                        }

                        if (_tryCounter < _maxTriesCount)
                        {
                            StateChangedAdvertisement(oldState: EFirmwareInstallationState.Uploading, newState: EFirmwareInstallationState.Error);
                            FatalErrorOccurredAdvertisement(EFirmwareInstallationState.Uploading, EFirmwareInstallerFatalErrorType.FirmwareUploadingErroredOut, "error while uploading firmware blah blah"); //  order
                            return;
                        }

                        FirmwareUploadProgressPercentageAndDataThroughputChangedAdvertisement(progressPercentage: 20, averageThroughput: 10);
                        await Task.Delay(10);
                        FirmwareUploadProgressPercentageAndDataThroughputChangedAdvertisement(progressPercentage: 40, averageThroughput: 10);
                        await Task.Delay(10);
                        FirmwareUploadProgressPercentageAndDataThroughputChangedAdvertisement(progressPercentage: 60, averageThroughput: 10);
                        await Task.Delay(10);
                        FirmwareUploadProgressPercentageAndDataThroughputChangedAdvertisement(progressPercentage: 80, averageThroughput: 10);
                        await Task.Delay(10);
                        FirmwareUploadProgressPercentageAndDataThroughputChangedAdvertisement(progressPercentage: 100, averageThroughput: 10);
                        await Task.Delay(10);
                    }

                    StateChangedAdvertisement(oldState: EFirmwareInstallationState.Uploading, newState: EFirmwareInstallationState.Testing);
                    await Task.Delay(10);
                    
                    StateChangedAdvertisement(oldState: EFirmwareInstallationState.Testing, newState: EFirmwareInstallationState.Confirming);
                    await Task.Delay(10);
                    
                    StateChangedAdvertisement(oldState: EFirmwareInstallationState.Confirming, newState: EFirmwareInstallationState.Resetting);
                    await Task.Delay(10);
                    
                    StateChangedAdvertisement(oldState: EFirmwareInstallationState.Resetting, newState: EFirmwareInstallationState.Complete);
                });

                return verdict;

                //00 simulating the state changes in a background thread is vital in order to simulate the async nature of the native uploader
            }
        }
    }
}