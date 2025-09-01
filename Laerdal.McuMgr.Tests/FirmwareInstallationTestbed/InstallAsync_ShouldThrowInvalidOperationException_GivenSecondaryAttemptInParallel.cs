using FluentAssertions;
using Laerdal.McuMgr.FirmwareInstallation;
using Laerdal.McuMgr.FirmwareInstallation.Contracts.Enums;
using Laerdal.McuMgr.FirmwareInstallation.Contracts.Events;
using Laerdal.McuMgr.FirmwareInstallation.Contracts.Native;
using GenericNativeFirmwareInstallerCallbacksProxy_ = Laerdal.McuMgr.FirmwareInstallation.FirmwareInstaller.GenericNativeFirmwareInstallerCallbacksProxy;

#pragma warning disable xUnit1026

namespace Laerdal.McuMgr.Tests.FirmwareInstallationTestbed
{
    public partial class FirmwareInstallerTestbed
    {
        [Theory] //@formatter:off                 task1UsesAsyncNotBeginInstall    task2UsesAsyncNotBeginInstall
        [InlineData("FDT.IA.STIOE.GSAIP.010",     true,                            true                           )]
        [InlineData("FDT.IA.STIOE.GSAIP.020",     true,                            false                          )]
        [InlineData("FDT.IA.STIOE.GSAIP.030",     false,                           false                          )] //this case must also be checked here  @formatter:on
        public async Task InstallAsync_ShouldThrowInvalidOperationException_GivenSecondaryAttemptInParallel(string testcaseNickname, bool task1UsesAsyncNotBeginInstall, bool task2UsesAsyncNotBeginInstall)
        {
            // Arrange
            var mockedNativeFirmwareInstallerProxy = new MockedGreenNativeFirmwareInstallerProxySpy90(new GenericNativeFirmwareInstallerCallbacksProxy_());
            var firmwareInstaller = new FirmwareInstallerSpy90(mockedNativeFirmwareInstallerProxy);

            using var eventsMonitor = firmwareInstaller.Monitor();

            // Act
            var work = new Func<Task>(async () =>
            {
                var taskParkingGuard = new ManualResetEventSlim(initialState: false);
             
                var task1ReadyGuard = new ManualResetEventSlim(initialState: false);
                var racingTask1 = Task.Run(async () =>
                {
                    task1ReadyGuard.Set(); //signal that task1 is parked and ready
                    taskParkingGuard.Wait(); //parking both tasks at the start
                    
                    if (task1UsesAsyncNotBeginInstall)
                    {
                        await firmwareInstaller.InstallAsync(
                            data: [1, 2, 3],
                            maxTriesCount: 1,
                            hostDeviceModel: "foobar",
                            hostDeviceManufacturer: "acme corp."
                        );
                    }
                    else
                    {
                        firmwareInstaller.BeginInstallation(
                            data: [1, 2, 3],
                            hostDeviceModel: "foobar",
                            hostDeviceManufacturer: "acme corp."
                        );    
                    }
                });
                
                var task2ReadyGuard = new ManualResetEventSlim(initialState: false);
                var racingTask2 = Task.Run(async () =>
                {
                    task2ReadyGuard.Set(); //signal that task2 is parked and ready
                    taskParkingGuard.Wait(); //parking both tasks at the start
                    
                    if (task2UsesAsyncNotBeginInstall)
                    {
                        await firmwareInstaller.InstallAsync(
                            data: [4, 5, 6],
                            maxTriesCount: 1,
                            hostDeviceModel: "foobar",
                            hostDeviceManufacturer: "acme corp."
                        );
                    }
                    else
                    {
                        firmwareInstaller.BeginInstallation(
                            data: [4, 5, 6],
                            hostDeviceModel: "foobar",
                            hostDeviceManufacturer: "acme corp."
                        );    
                    }
                });
                
                await Task.Yield(); //order      just to be 100% sure that the tasks above will be launched before we set the event
                
                task1ReadyGuard.Wait(); //order  wait until both tasks
                task2ReadyGuard.Wait(); //order  are parked and ready
                
                taskParkingGuard.Set(); //order  and finally start the core-logic of the two tasks at exactly the same time

                await Task.WhenAll(racingTask1, racingTask2); // let them race   one of the two should throw InvalidOperationException
                
                if (racingTask1.IsCompletedSuccessfully && racingTask2.IsCompletedSuccessfully)
                    throw new Exception("Both tasks completed successfully, which is unexpected.");
                
                if (racingTask1.IsFaulted && racingTask2.IsFaulted)
                    throw new Exception("Both tasks errored-out, which is unexpected.");
                
                throw (racingTask1.IsFaulted ? racingTask1.Exception : racingTask2.Exception)!;
            });

            // Assert
            await work.Should().ThrowWithinAsync<InvalidOperationException>(TimeSpan.FromSeconds(2));

            firmwareInstaller //we need to be 100% sure that the guard check was called by both racing tasks
                .GuardCallsCounter
                .Should().Be(2);
            
            firmwareInstaller //we need to be 100% sure that the guard check is the one that threw the exception!
                .InvalidOperationExceptionThrownByGuardCheckCounter
                .Should().Be(1);
            
            firmwareInstaller //we need to be 100% sure that the token was released only once (by the task that started the installation process)
                .ReleaseCallsCounter
                .Should().Be(1);
            
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
        
        private class FirmwareInstallerSpy90 : FirmwareInstaller
        {
            public volatile int GuardCallsCounter;
            public volatile int ReleaseCallsCounter;
            public volatile int InvalidOperationExceptionThrownByGuardCheckCounter;
            
            public FirmwareInstallerSpy90(INativeFirmwareInstallerProxy nativeFirmwareInstallerProxy) : base(nativeFirmwareInstallerProxy)
            {
            }

            protected override void EnsureExclusiveOperationToken() //we need this spy in order to be 100% sure that the guard check is the one that threw the exception!
            {
                Interlocked.Increment(ref GuardCallsCounter);
                
                try
                {
                    base.EnsureExclusiveOperationToken();
                }
                catch (InvalidOperationException)
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

        private class MockedGreenNativeFirmwareInstallerProxySpy90 : MockedNativeFirmwareInstallerProxySpy
        {
            public MockedGreenNativeFirmwareInstallerProxySpy90(INativeFirmwareInstallerCallbacksProxy firmwareInstallerCallbacksProxy)
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