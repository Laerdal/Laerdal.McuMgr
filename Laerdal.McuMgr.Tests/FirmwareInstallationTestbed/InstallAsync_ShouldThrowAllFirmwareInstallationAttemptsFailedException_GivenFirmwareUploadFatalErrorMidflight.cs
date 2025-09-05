using FluentAssertions;
using FluentAssertions.Extensions;
using Laerdal.McuMgr.Common.Enums;
using Laerdal.McuMgr.FirmwareInstallation;
using Laerdal.McuMgr.FirmwareInstallation.Contracts.Enums;
using Laerdal.McuMgr.FirmwareInstallation.Contracts.Events;
using Laerdal.McuMgr.FirmwareInstallation.Contracts.Exceptions;
using Laerdal.McuMgr.FirmwareInstallation.Contracts.Native;
using GenericNativeFirmwareInstallerCallbacksProxy_ = Laerdal.McuMgr.FirmwareInstallation.FirmwareInstaller.GenericNativeFirmwareInstallerCallbacksProxy;

namespace Laerdal.McuMgr.Tests.FirmwareInstallationTestbed
{
    public partial class FirmwareInstallerTestbed
    {
        [Fact]
        public async Task InstallAsync_ShouldThrowAllFirmwareInstallationAttemptsFailedException_GivenFirmwareUploadFatalErrorMidflight()
        {
            // Arrange
            var mockedNativeFirmwareInstallerProxy = new MockedGreenNativeFirmwareInstallerProxySpy6(new GenericNativeFirmwareInstallerCallbacksProxy_());
            var firmwareInstaller = new FirmwareInstaller(mockedNativeFirmwareInstallerProxy);

            using var eventsMonitor = firmwareInstaller.Monitor();

            // Act
            var work = new Func<Task>(() => firmwareInstaller.InstallAsync(
                hostDeviceModel: "foobar",
                hostDeviceManufacturer: "acme corp.",
                
                data: [1, 2, 3],
                maxTriesCount: 2,
                sleepTimeBetweenRetriesInMs: 0
            ));

            // Assert
            (
                await work.Should().ThrowWithinAsync<AllFirmwareInstallationAttemptsFailedException>(3_000.Milliseconds())
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

            public override EFirmwareInstallationVerdict NativeBeginInstallation(
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
                base.NativeBeginInstallation(
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
                
                StateChangedAdvertisement(EFirmwareInstallationState.None, EFirmwareInstallationState.None);
                StateChangedAdvertisement(EFirmwareInstallationState.Idle, EFirmwareInstallationState.Idle);
                
                Task.Run(async () => //00 vital
                {
                    StateChangedAdvertisement(EFirmwareInstallationState.Idle, EFirmwareInstallationState.Validating);
                    await Task.Delay(100);
                    
                    StateChangedAdvertisement(EFirmwareInstallationState.Validating, EFirmwareInstallationState.Uploading);
                    await Task.Delay(100);
                    
                    StateChangedAdvertisement(EFirmwareInstallationState.Uploading, EFirmwareInstallationState.Testing);
                    await Task.Delay(100);
                    
                    StateChangedAdvertisement(EFirmwareInstallationState.Uploading, EFirmwareInstallationState.Error); //                                                                                                          order
                    FatalErrorOccurredAdvertisement(EFirmwareInstallationState.Uploading, EFirmwareInstallerFatalErrorType.FirmwareUploadingErroredOut, "error while uploading firmware blah blah", EGlobalErrorCode.Generic); //  order
                });

                return EFirmwareInstallationVerdict.Success;

                //00 simulating the state changes in a background thread is vital in order to simulate the async nature of the native uploader
            }
        }
    }
}