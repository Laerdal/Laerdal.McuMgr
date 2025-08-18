using FluentAssertions;
using FluentAssertions.Extensions;
using Laerdal.McuMgr.Common.Constants;
using Laerdal.McuMgr.Common.Enums;
using Laerdal.McuMgr.Common.Events;
using Laerdal.McuMgr.FirmwareInstallation;
using Laerdal.McuMgr.FirmwareInstallation.Contracts.Enums;
using Laerdal.McuMgr.FirmwareInstallation.Contracts.Events;
using Laerdal.McuMgr.FirmwareInstallation.Contracts.Native;
using GenericNativeFirmwareInstallerCallbacksProxy_ = Laerdal.McuMgr.FirmwareInstallation.FirmwareInstaller.GenericNativeFirmwareInstallerCallbacksProxy;

#pragma warning disable xUnit1026

namespace Laerdal.McuMgr.Tests.FirmwareInstallationTestbed
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
        [InlineData("FIT.IA.SCSWLDAULIM.GPDFFU.010", 2, false)]
        [InlineData("FIT.IA.SCSWLDAULIM.GPDFFU.020", 3, false)]
        [InlineData("FIT.IA.SCSWLDAULIM.GPDFFU.030", 5, false)]
        [InlineData("FIT.IA.SCSWLDAULIM.GPDFFU.035", 5, true)] //simulateUserlandExceptionsInEventHandlers
        public async Task InstallAsync_ShouldCompleteSuccessfullyWithLastDitchAttemptUsingLowerInitialMtu_GivenFlakyConnectionForFileUploading(string testNickname, int maxTriesCount, bool simulateUserlandExceptionsInEventHandlers)
        {
            // Arrange
            var allLogEas = new List<LogEmittedEventArgs>(8);
            var mockedNativeFirmwareInstallerProxy = new MockedGreenNativeFirmwareInstallerProxySpy10(new GenericNativeFirmwareInstallerCallbacksProxy_(), maxTriesCount);
            var firmwareInstaller = new FirmwareInstaller(mockedNativeFirmwareInstallerProxy);
            
            using var eventsMonitor = firmwareInstaller.Monitor(); //order

            firmwareInstaller.LogEmitted += (object _, in LogEmittedEventArgs ea) => allLogEas.Add(ea); //keep first

            if (simulateUserlandExceptionsInEventHandlers)
            {
                firmwareInstaller.Cancelled += (_, _) => throw new Exception($"{nameof(firmwareInstaller.Cancelled)} -> oops!"); //order   these must be wired up after the events-monitor
                firmwareInstaller.LogEmitted += (object _, in LogEmittedEventArgs _) => throw new Exception($"{nameof(firmwareInstaller.LogEmitted)} -> oops!");
                firmwareInstaller.StateChanged += (_, _) => throw new Exception($"{nameof(firmwareInstaller.StateChanged)} -> oops!");
                firmwareInstaller.BusyStateChanged += (_, _) => throw new Exception($"{nameof(firmwareInstaller.BusyStateChanged)} -> oops!");
                firmwareInstaller.FatalErrorOccurred += (_, _) => throw new Exception($"{nameof(firmwareInstaller.FatalErrorOccurred)} -> oops!");
                firmwareInstaller.IdenticalFirmwareCachedOnTargetDeviceDetected += (_, _) => throw new Exception($"{nameof(firmwareInstaller.IdenticalFirmwareCachedOnTargetDeviceDetected)} -> oops!");
                firmwareInstaller.FirmwareUploadProgressPercentageAndDataThroughputChanged += (_, _) => throw new Exception($"{nameof(firmwareInstaller.FirmwareUploadProgressPercentageAndDataThroughputChanged)} -> oops!");    
            }

            // Act
            var work = new Func<Task>(() => firmwareInstaller.InstallAsync(
                data: [1, 2, 3],
                maxTriesCount: maxTriesCount,
                hostDeviceModel: "foobar",
                hostDeviceManufacturer: "acme corp."
            ));

            // Assert
            await work.Should().CompleteWithinAsync(40000.Seconds());
            
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
                .Where(x => x.EventName == nameof(firmwareInstaller.StateChanged))
                .Count(x => x.Parameters.OfType<StateChangedEventArgs>().FirstOrDefault() is { NewState: EFirmwareInstallationState.Complete })
                .Should()
                .Be(1);

            // eventsMonitor
            //     .OccurredEvents
            //     .Where(x => x.EventName == nameof(deviceResetter.LogEmitted))
            //     .SelectMany(x => x.Parameters)
            //     .OfType<LogEmittedEventArgs>() //xunit or fluent-assertions has memory corruption issues with this probably because of the zero-copy delegate! :(

            allLogEas
                .Count(l => l is {Level: ELogLevel.Warning} && l.Message.Contains("[FI.IA.010]", StringComparison.InvariantCulture))
                .Should()
                .BeGreaterThanOrEqualTo(1);

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
                    estimatedSwapTimeInMilliseconds: estimatedSwapTimeInMilliseconds,
                    
                    initialMtuSize: initialMtuSize,
                    
                    pipelineDepth: pipelineDepth,
                    byteAlignment: byteAlignment,
                    windowCapacity: windowCapacity,
                    memoryAlignment: memoryAlignment
                );

                Task.Run(function: async () => //00 vital
                {
                    try
                    {
                        if (_tryCounter == 1 && initialMtuSize == AndroidTidbits.BleConnectionFailsafeSettings.ForUploading.InitialMtuSize)
                        {
                            BugDetected = $"[BUG DETECTED] The very first try should not be with {nameof(initialMtuSize)} set to the fail-safe value of {AndroidTidbits.BleConnectionFailsafeSettings.ForUploading.InitialMtuSize} right off the bat - something is wrong!";
                            StateChangedAdvertisement(oldState: EFirmwareInstallationState.Uploading, newState: EFirmwareInstallationState.Error);
                            FatalErrorOccurredAdvertisement(EFirmwareInstallationState.Uploading, EFirmwareInstallerFatalErrorType.FirmwareUploadingErroredOut, BugDetected, EGlobalErrorCode.Generic);
                            return;
                        }

                        if (_tryCounter == 1 && windowCapacity == AndroidTidbits.BleConnectionFailsafeSettings.ForUploading.WindowCapacity)
                        {
                            BugDetected = $"[BUG DETECTED] The very first try should not be with {nameof(windowCapacity)} set to the fail-safe value of {AndroidTidbits.BleConnectionFailsafeSettings.ForUploading.WindowCapacity} right off the bat - something is wrong!";
                            StateChangedAdvertisement(oldState: EFirmwareInstallationState.Uploading, newState: EFirmwareInstallationState.Error);
                            FatalErrorOccurredAdvertisement(EFirmwareInstallationState.Uploading, EFirmwareInstallerFatalErrorType.FirmwareUploadingErroredOut, BugDetected, EGlobalErrorCode.Generic);
                            return;
                        }

                        if (_tryCounter == 1 && memoryAlignment == AndroidTidbits.BleConnectionFailsafeSettings.ForUploading.MemoryAlignment)
                        {
                            BugDetected = $"[BUG DETECTED] The very first try should not be with {nameof(memoryAlignment)} set to the fail-safe value of {AndroidTidbits.BleConnectionFailsafeSettings.ForUploading.MemoryAlignment} right off the bat - something is wrong!";
                            StateChangedAdvertisement(oldState: EFirmwareInstallationState.Uploading, newState: EFirmwareInstallationState.Error);
                            FatalErrorOccurredAdvertisement(EFirmwareInstallationState.Uploading, EFirmwareInstallerFatalErrorType.FirmwareUploadingErroredOut, BugDetected, EGlobalErrorCode.Generic);
                            return;
                        }

                        if (_tryCounter == 1 && pipelineDepth == AppleTidbits.BleConnectionFailsafeSettings.ForUploading.PipelineDepth)
                        {
                            BugDetected = $"[BUG DETECTED] The very first try should not be with {nameof(pipelineDepth)} set to the fail-safe value of {AppleTidbits.BleConnectionFailsafeSettings.ForUploading.PipelineDepth} right off the bat - something is wrong!";
                            StateChangedAdvertisement(oldState: EFirmwareInstallationState.Uploading, newState: EFirmwareInstallationState.Error);
                            FatalErrorOccurredAdvertisement(EFirmwareInstallationState.Uploading, EFirmwareInstallerFatalErrorType.FirmwareUploadingErroredOut, BugDetected, EGlobalErrorCode.Generic);
                            return;
                        }

                        if (_tryCounter == 1 && byteAlignment == AppleTidbits.BleConnectionFailsafeSettings.ForUploading.ByteAlignment)
                        {
                            BugDetected = $"[BUG DETECTED] The very first try should not be with {nameof(byteAlignment)} set to the fail-safe value of {AppleTidbits.BleConnectionFailsafeSettings.ForUploading.ByteAlignment} right off the bat - something is wrong!";
                            StateChangedAdvertisement(oldState: EFirmwareInstallationState.Uploading, newState: EFirmwareInstallationState.Error);
                            FatalErrorOccurredAdvertisement(EFirmwareInstallationState.Uploading, EFirmwareInstallerFatalErrorType.FirmwareUploadingErroredOut, BugDetected, EGlobalErrorCode.Generic);
                            return;
                        }

                        StateChangedAdvertisement(oldState: EFirmwareInstallationState.Idle, newState: EFirmwareInstallationState.Idle);
                        await Task.Delay(10);

                        StateChangedAdvertisement(oldState: EFirmwareInstallationState.Idle, newState: EFirmwareInstallationState.Validating);
                        await Task.Delay(10);

                        StateChangedAdvertisement(oldState: EFirmwareInstallationState.Validating, newState: EFirmwareInstallationState.Uploading);
                        await Task.Delay(100);

                        {
                            //file uploading simulation
                            FirmwareUploadProgressPercentageAndDataThroughputChangedAdvertisement(progressPercentage: 00, currentThroughputInKBps: 00, totalAverageThroughputInKBps: 00);
                            await Task.Delay(10);

                            if (_tryCounter == _maxTriesCount && initialMtuSize == null)
                            {
                                BugDetected = $"[BUG DETECTED] The very last try should be with {nameof(initialMtuSize)} set to a fail-safe value but it's still set to 'null' - something is wrong!";
                                StateChangedAdvertisement(oldState: EFirmwareInstallationState.Uploading, newState: EFirmwareInstallationState.Error);
                                FatalErrorOccurredAdvertisement(EFirmwareInstallationState.Uploading, EFirmwareInstallerFatalErrorType.FirmwareUploadingErroredOut, BugDetected, EGlobalErrorCode.Generic);
                                return;
                            }

                            if (_tryCounter == _maxTriesCount && windowCapacity == null)
                            {
                                BugDetected = $"[BUG DETECTED] The very last try should be with {nameof(windowCapacity)} set to a fail-safe value but it's still set to 'null' - something is wrong!";
                                StateChangedAdvertisement(oldState: EFirmwareInstallationState.Uploading, newState: EFirmwareInstallationState.Error);
                                FatalErrorOccurredAdvertisement(EFirmwareInstallationState.Uploading, EFirmwareInstallerFatalErrorType.FirmwareUploadingErroredOut, BugDetected, EGlobalErrorCode.Generic);
                                return;
                            }

                            if (_tryCounter == _maxTriesCount && memoryAlignment == null)
                            {
                                BugDetected = $"[BUG DETECTED] The very last try should be with {nameof(memoryAlignment)} set to a fail-safe value but it's still set to 'null' - something is wrong!";
                                StateChangedAdvertisement(oldState: EFirmwareInstallationState.Uploading, newState: EFirmwareInstallationState.Error);
                                FatalErrorOccurredAdvertisement(EFirmwareInstallationState.Uploading, EFirmwareInstallerFatalErrorType.FirmwareUploadingErroredOut, BugDetected, EGlobalErrorCode.Generic);
                                return;
                            }

                            if (_tryCounter == _maxTriesCount && pipelineDepth == null)
                            {
                                BugDetected = $"[BUG DETECTED] The very last try should be with {nameof(pipelineDepth)} set to a fail-safe value but it's still set to 'null' - something is wrong!";
                                StateChangedAdvertisement(oldState: EFirmwareInstallationState.Uploading, newState: EFirmwareInstallationState.Error);
                                FatalErrorOccurredAdvertisement(EFirmwareInstallationState.Uploading, EFirmwareInstallerFatalErrorType.FirmwareUploadingErroredOut, BugDetected, EGlobalErrorCode.Generic);
                                return;
                            }

                            if (_tryCounter == _maxTriesCount && byteAlignment == null)
                            {
                                BugDetected = $"[BUG DETECTED] The very last try should be with {nameof(byteAlignment)} set to a fail-safe value but it's still set to 'null' - something is wrong!";
                                StateChangedAdvertisement(oldState: EFirmwareInstallationState.Uploading, newState: EFirmwareInstallationState.Error);
                                FatalErrorOccurredAdvertisement(EFirmwareInstallationState.Uploading, EFirmwareInstallerFatalErrorType.FirmwareUploadingErroredOut, BugDetected, EGlobalErrorCode.Generic);
                                return;
                            }

                            if (_tryCounter < _maxTriesCount)
                            {
                                StateChangedAdvertisement(oldState: EFirmwareInstallationState.Uploading, newState: EFirmwareInstallationState.Error);
                                FatalErrorOccurredAdvertisement(EFirmwareInstallationState.Uploading, EFirmwareInstallerFatalErrorType.FirmwareUploadingErroredOut, "error while uploading firmware blah blah", EGlobalErrorCode.Generic); //  order
                                return;
                            }

                            FirmwareUploadProgressPercentageAndDataThroughputChangedAdvertisement(progressPercentage: 20, currentThroughputInKBps: 10, totalAverageThroughputInKBps: 10);
                            await Task.Delay(10);
                            FirmwareUploadProgressPercentageAndDataThroughputChangedAdvertisement(progressPercentage: 40, currentThroughputInKBps: 10, totalAverageThroughputInKBps: 10);
                            await Task.Delay(10);
                            FirmwareUploadProgressPercentageAndDataThroughputChangedAdvertisement(progressPercentage: 60, currentThroughputInKBps: 10, totalAverageThroughputInKBps: 10);
                            await Task.Delay(10);
                            FirmwareUploadProgressPercentageAndDataThroughputChangedAdvertisement(progressPercentage: 80, currentThroughputInKBps: 10, totalAverageThroughputInKBps: 10);
                            await Task.Delay(10);
                            FirmwareUploadProgressPercentageAndDataThroughputChangedAdvertisement(progressPercentage: 100, currentThroughputInKBps: 10, totalAverageThroughputInKBps: 10);
                            await Task.Delay(10);
                        }

                        StateChangedAdvertisement(oldState: EFirmwareInstallationState.Uploading, newState: EFirmwareInstallationState.Testing);
                        await Task.Delay(10);

                        StateChangedAdvertisement(oldState: EFirmwareInstallationState.Testing, newState: EFirmwareInstallationState.Resetting);
                        await Task.Delay(10);
                        
                        StateChangedAdvertisement(oldState: EFirmwareInstallationState.Resetting, newState: EFirmwareInstallationState.Confirming);
                        await Task.Delay(10);

                        StateChangedAdvertisement(oldState: EFirmwareInstallationState.Confirming, newState: EFirmwareInstallationState.Complete);
                    }
                    catch (Exception ex)
                    {
                        BugDetected = $"[BUG DETECTED] Detected an unexpected exception in the mock class (probably from an event-handler) - this shouldn't happen! Exception: {ex.Message}";
                        StateChangedAdvertisement(oldState: EFirmwareInstallationState.Uploading, newState: EFirmwareInstallationState.Error);
                        FatalErrorOccurredAdvertisement(EFirmwareInstallationState.Uploading, EFirmwareInstallerFatalErrorType.FirmwareUploadingErroredOut, BugDetected, EGlobalErrorCode.Generic);
                    }
                });

                return verdict;

                //00 simulating the state changes in a background thread is vital in order to simulate the async nature of the native uploader
            }
        }
    }
}