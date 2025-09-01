using System.ComponentModel;
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
        [Fact]
        public async Task InstallAsync_ShouldThrowInvalidOperationException_GivenSecondaryAttemptInParallel()
        {
            // Arrange
            var mockedNativeFirmwareInstallerProxy = new MockedGreenNativeFirmwareInstallerProxySpy90(new GenericNativeFirmwareInstallerCallbacksProxy_());
            var firmwareInstaller = new FirmwareInstaller(mockedNativeFirmwareInstallerProxy);

            using var eventsMonitor = firmwareInstaller.Monitor();

            // Act
            var work = new Func<Task>(async () =>
            {
                var racingTask1 = firmwareInstaller.InstallAsync(
                    data: [1, 2, 3],
                    maxTriesCount: 1,
                    hostDeviceModel: "foobar",
                    hostDeviceManufacturer: "acme corp."
                );
                
                var racingTask2 = firmwareInstaller.InstallAsync(
                    data: [4, 5, 6],
                    maxTriesCount: 1,
                    hostDeviceModel: "foobar",
                    hostDeviceManufacturer: "acme corp."
                );

                await Task.WhenAll(racingTask1, racingTask2); // let them race   one of the two should throw InvalidOperationException
                
                if (racingTask1.IsCompletedSuccessfully && racingTask2.IsCompletedSuccessfully)
                    throw new Exception("Both tasks completed successfully, which is unexpected.");
                
                if (racingTask1.IsFaulted && racingTask2.IsFaulted)
                    throw new Exception("Both tasks completed successfully, which is unexpected.");
                
                throw (racingTask1.IsFaulted ? racingTask1.Exception : racingTask2.Exception)!;
            });

            // Assert
            await work.Should().ThrowWithinAsync<InvalidOperationException>(TimeSpan.FromSeconds(2));

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

        private class MockedGreenNativeFirmwareInstallerProxySpy90 : MockedNativeFirmwareInstallerProxySpy
        {
            public MockedGreenNativeFirmwareInstallerProxySpy90(INativeFirmwareInstallerCallbacksProxy firmwareInstallerCallbacksProxy)
                : base(firmwareInstallerCallbacksProxy)
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

                Task.Run(function: async () => //00 vital
                {
                    StateChangedAdvertisement(oldState: EFirmwareInstallationState.None, newState: EFirmwareInstallationState.None);
                    await Task.Delay(10);
                    
                    StateChangedAdvertisement(oldState: EFirmwareInstallationState.None, newState: EFirmwareInstallationState.Idle);
                    await Task.Delay(10);
                    
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

                return verdict;

                //00 simulating the state changes in a background thread is vital in order to simulate the async nature of the native uploader
            }
        }
    }
}