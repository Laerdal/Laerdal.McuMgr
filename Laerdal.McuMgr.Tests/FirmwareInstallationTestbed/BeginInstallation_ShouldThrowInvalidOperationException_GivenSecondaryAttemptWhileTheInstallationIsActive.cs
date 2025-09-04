using FluentAssertions;
using FluentAssertions.Extensions;
using Laerdal.McuMgr.FirmwareInstallation;
using Laerdal.McuMgr.FirmwareInstallation.Contracts.Enums;
using Laerdal.McuMgr.FirmwareInstallation.Contracts.Events;
using Laerdal.McuMgr.FirmwareInstallation.Contracts.Exceptions;
using Laerdal.McuMgr.FirmwareInstallation.Contracts.Native;
using GenericNativeFirmwareInstallerCallbacksProxy_ = Laerdal.McuMgr.FirmwareInstallation.FirmwareInstaller.GenericNativeFirmwareInstallerCallbacksProxy;

#pragma warning disable xUnit1026

namespace Laerdal.McuMgr.Tests.FirmwareInstallationTestbed
{
    public partial class FirmwareInstallerTestbed
    {
        [Fact]
        public async Task BeginInstallation_ShouldThrowInvalidOperationException_GivenSecondaryAttemptWhileTheInstallationIsActive()
        {
            // Arrange
            var mockedNativeFirmwareInstallerProxy = new MockedGreenNativeFirmwareInstallerProxySpy100(new GenericNativeFirmwareInstallerCallbacksProxy_());
            var firmwareInstaller = new FirmwareInstallerSpy100(mockedNativeFirmwareInstallerProxy);

            using var eventsMonitor = firmwareInstaller.Monitor();

            // Act
            var work = new Func<Task>(async () =>
            {
                var manualResetEvent = new ManualResetEventSlim(initialState: false);
                
                var racingTask = Task.Run(async () =>
                {
                    await Task.CompletedTask;
                
                    manualResetEvent.Wait(); //parking it until the first installation starts validating+uploading

                    firmwareInstaller.BeginInstallation(
                        data: [4, 5, 6],
                        hostDeviceModel: "foobar",
                        hostDeviceManufacturer: "acme corp."
                    );
                });
                
                await Task.Yield();
                
                firmwareInstaller.StateChanged += (_, ea_) =>
                {
                    if (ea_.NewState == EFirmwareInstallationState.Validating)
                    {
                        manualResetEvent.Set(); // tell the 'racingTask' to start exactly now
                        Thread.Sleep(100); //      stall the first installation a bit for good measure
                    }
                };
                
                firmwareInstaller.BeginInstallation(
                    data: [1, 2, 3],
                    hostDeviceModel: "foobar",
                    hostDeviceManufacturer: "acme corp."
                );
                
                await racingTask;
            });

            // Assert
            (await work.Should().ThrowWithinAsync<InvalidOperationException>(1.Days())).WithMessage("*another installation is already in progress*");

            firmwareInstaller //we need to be 100% sure that the guard check was called by both racing tasks
                .GuardCallsCounter
                .Should().Be(2);
            
            //firmwareInstaller.ReleaseCallsCounter.Should().Be(1); //irrelevant for this test
            
            firmwareInstaller  //it must be zero because this test is not aiming at the guard-check   it aims at 'IsCold()' in the native-layer!
                .InvalidOperationExceptionThrownByGuardCheckCounter
                .Should().Be(0);
            
            mockedNativeFirmwareInstallerProxy.CancelCalled.Should().BeFalse();
            mockedNativeFirmwareInstallerProxy.DisconnectCalled.Should().BeFalse(); //00
            mockedNativeFirmwareInstallerProxy.BeginInstallationCalled.Should().BeTrue();

            eventsMonitor
                .OccurredEvents
                .Count(x => x.EventName == nameof(firmwareInstaller.StateChanged)
                            && x.Parameters.OfType<StateChangedEventArgs>().Any(ea => ea.NewState == EFirmwareInstallationState.Idle))
                .Should()
                .Be(1); // only one of the two tasks should have started the installation process

            //00 we dont want to disconnect the device regardless of the outcome
        }
        
        private class FirmwareInstallerSpy100 : FirmwareInstaller
        {
            public volatile int GuardCallsCounter = 0;
            public volatile int ReleaseCallsCounter = 0;
            public volatile int InvalidOperationExceptionThrownByGuardCheckCounter = 0;
            
            public FirmwareInstallerSpy100(INativeFirmwareInstallerProxy nativeFirmwareInstallerProxy) : base(nativeFirmwareInstallerProxy)
            {
            }

            protected override void EnsureExclusiveOperationToken() //we need this spy in order to be 100% sure that the guard check is the one that threw the exception!
            {
                Interlocked.Increment(ref GuardCallsCounter);
                
                try
                {
                    base.EnsureExclusiveOperationToken();    
                }
                catch (AnotherFirmwareInstallationIsAlreadyOngoingException)
                {
                    Interlocked.Increment(ref InvalidOperationExceptionThrownByGuardCheckCounter);
                    throw;
                }
            }

            protected override void ReleaseExclusiveOperationToken()
            {
                Interlocked.Increment(ref ReleaseCallsCounter);
                
                base.ReleaseExclusiveOperationToken();
            }
        }

        private class MockedGreenNativeFirmwareInstallerProxySpy100 : MockedNativeFirmwareInstallerProxySpy
        {
            public MockedGreenNativeFirmwareInstallerProxySpy100(INativeFirmwareInstallerCallbacksProxy firmwareInstallerCallbacksProxy)
                : base(firmwareInstallerCallbacksProxy)
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

                StateChangedAdvertisement(oldState: EFirmwareInstallationState.None, newState: EFirmwareInstallationState.None); //must be outside the Task.Run()
                Thread.Sleep(010);

                StateChangedAdvertisement(oldState: EFirmwareInstallationState.None, newState: EFirmwareInstallationState.Idle); //must be outside the Task.Run()
                Thread.Sleep(500); //this delay needs to be a bit hefty for the sake of the beginInstall() race!
                
                Task.Run(function: async () => //00 vital
                {
                    StateChangedAdvertisement(oldState: EFirmwareInstallationState.Idle, newState: EFirmwareInstallationState.Validating);
                    await Task.Delay(10);
                    
                    StateChangedAdvertisement(oldState: EFirmwareInstallationState.Validating, newState: EFirmwareInstallationState.Uploading);
                    await Task.Delay(100);
                    
                    FirmwareUploadProgressPercentageAndDataThroughputChangedAdvertisement(progressPercentage: 00, currentThroughputInKBps: 00, totalAverageThroughputInKBps: 00);
                    await Task.Delay(10);
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
                    
                    StateChangedAdvertisement(oldState: EFirmwareInstallationState.Uploading, newState: EFirmwareInstallationState.Testing);
                    await Task.Delay(10);

                    StateChangedAdvertisement(oldState: EFirmwareInstallationState.Testing, newState: EFirmwareInstallationState.Resetting);
                    await Task.Delay(10);

                    StateChangedAdvertisement(oldState: EFirmwareInstallationState.Resetting, newState: EFirmwareInstallationState.Confirming);
                    await Task.Delay(10);

                    StateChangedAdvertisement(oldState: EFirmwareInstallationState.Confirming, newState: EFirmwareInstallationState.Complete);
                });

                return EFirmwareInstallationVerdict.Success;

                //00 simulating the state changes in a background thread is vital in order to simulate the async nature of the native uploader
            }
        }
    }
}