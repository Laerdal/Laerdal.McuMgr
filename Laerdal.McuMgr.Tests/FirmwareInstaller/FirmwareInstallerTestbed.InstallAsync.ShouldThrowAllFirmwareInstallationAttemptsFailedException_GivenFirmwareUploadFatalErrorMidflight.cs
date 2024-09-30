using FluentAssertions;
using Laerdal.McuMgr.Common.Helpers;
using Laerdal.McuMgr.FirmwareInstaller.Contracts.Enums;
using Laerdal.McuMgr.FirmwareInstaller.Contracts.Events;
using Laerdal.McuMgr.FirmwareInstaller.Contracts.Exceptions;
using Laerdal.McuMgr.FirmwareInstaller.Contracts.Native;
using GenericNativeFirmwareInstallerCallbacksProxy_ = Laerdal.McuMgr.FirmwareInstaller.FirmwareInstaller.GenericNativeFirmwareInstallerCallbacksProxy;

namespace Laerdal.McuMgr.Tests.FirmwareInstaller
{
    public partial class FirmwareInstallerTestbed
    {
        [Fact]
        public async Task InstallAsync_ShouldThrowAllFirmwareInstallationAttemptsFailedException_GivenFirmwareUploadFatalErrorMidflight()
        {
            // Arrange
            var mockedNativeFirmwareInstallerProxy = new MockedGreenNativeFirmwareInstallerProxySpy6(new GenericNativeFirmwareInstallerCallbacksProxy_());
            var firmwareInstaller = new McuMgr.FirmwareInstaller.FirmwareInstaller(mockedNativeFirmwareInstallerProxy);

            using var eventsMonitor = firmwareInstaller.Monitor();

            // Act
            var work = new Func<Task>(() => firmwareInstaller.InstallAsync(
                data: [1, 2, 3],
                maxTriesCount: 2,
                sleepTimeBetweenRetriesInMs: 0
            ));

            // Assert
            (
                await work.Should()
                    .ThrowExactlyAsync<AllFirmwareInstallationAttemptsFailedException>()
                    .WithTimeoutInMs(3_000)
            ).WithInnerException<FirmwareInstallationUploadingStageErroredOutException>();

            mockedNativeFirmwareInstallerProxy.CancelCalled.Should().BeFalse();
            mockedNativeFirmwareInstallerProxy.DisconnectCalled.Should().BeFalse(); //00
            mockedNativeFirmwareInstallerProxy.BeginInstallationCalled.Should().BeTrue();

            eventsMonitor.Should().NotRaise(nameof(firmwareInstaller.Cancelled));

            eventsMonitor.OccurredEvents
                .Count(
                    x => x.EventName == nameof(firmwareInstaller.StateChanged)
                         && x.Parameters?.OfType<StateChangedEventArgs>().FirstOrDefault().NewState == EFirmwareInstallationState.Uploading
                )
                .Should().Be(2);

            eventsMonitor.OccurredEvents
                .Count(x => x.EventName == nameof(firmwareInstaller.FatalErrorOccurred))
                .Should().Be(2);
            
            eventsMonitor
                .Should().Raise(nameof(firmwareInstaller.StateChanged))
                .WithSender(firmwareInstaller)
                .WithArgs<StateChangedEventArgs>(args => args.NewState == EFirmwareInstallationState.Uploading);
            
            eventsMonitor
                .Should().Raise(nameof(firmwareInstaller.StateChanged))
                .WithSender(firmwareInstaller)
                .WithArgs<StateChangedEventArgs>(args => args.NewState == EFirmwareInstallationState.Error);
            
            //00 we dont want to disconnect the device regardless of the outcome
        }

        private class MockedGreenNativeFirmwareInstallerProxySpy6 : MockedNativeFirmwareInstallerProxySpy
        {
            public MockedGreenNativeFirmwareInstallerProxySpy6(INativeFirmwareInstallerCallbacksProxy firmwareInstallerCallbacksProxy) : base(firmwareInstallerCallbacksProxy)
            {
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

                Task.Run(async () => //00 vital
                {
                    StateChangedAdvertisement(EFirmwareInstallationState.Idle, EFirmwareInstallationState.Idle);
                    await Task.Delay(100);

                    StateChangedAdvertisement(EFirmwareInstallationState.Idle, EFirmwareInstallationState.Validating);
                    await Task.Delay(100);
                    
                    StateChangedAdvertisement(EFirmwareInstallationState.Validating, EFirmwareInstallationState.Uploading);
                    await Task.Delay(100);
                    
                    StateChangedAdvertisement(EFirmwareInstallationState.Uploading, EFirmwareInstallationState.Testing);
                    await Task.Delay(100);
                    
                    StateChangedAdvertisement(EFirmwareInstallationState.Uploading, EFirmwareInstallationState.Error); //                                                                                order
                    FatalErrorOccurredAdvertisement(EFirmwareInstallationState.Uploading, EFirmwareInstallerFatalErrorType.FirmwareUploadingErroredOut, "error while uploading firmware blah blah"); //  order
                });

                return verdict;

                //00 simulating the state changes in a background thread is vital in order to simulate the async nature of the native uploader
            }
        }
    }
}