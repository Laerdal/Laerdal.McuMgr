using System;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Extensions;
using Laerdal.McuMgr.FirmwareInstaller.Contracts.Enums;
using Laerdal.McuMgr.FirmwareInstaller.Contracts.Events;
using Laerdal.McuMgr.FirmwareInstaller.Contracts.Native;
using Xunit;
using GenericNativeFirmwareInstallerCallbacksProxy_ = Laerdal.McuMgr.FirmwareInstaller.FirmwareInstaller.GenericNativeFirmwareInstallerCallbacksProxy;

#pragma warning disable xUnit1026

namespace Laerdal.McuMgr.Tests.FirmwareInstaller
{
    public partial class FirmwareInstallerTestbed
    {
        [Fact]
        public async Task InstallAsync_ShouldCompleteSuccessfully_GivenGreenNativeFirmwareInstaller()
        {
            // Arrange
            var mockedNativeFirmwareInstallerProxy = new MockedGreenNativeFirmwareInstallerProxySpy(new GenericNativeFirmwareInstallerCallbacksProxy_());
            var firmwareInstaller = new McuMgr.FirmwareInstaller.FirmwareInstaller(mockedNativeFirmwareInstallerProxy);

            using var eventsMonitor = firmwareInstaller.Monitor();

            // Act
            var work = new Func<Task>(() => firmwareInstaller.InstallAsync(new byte[] { 1, 2, 3 }, maxRetriesCount: 0));

            // Assert
            await work.Should().CompleteWithinAsync(4.Seconds());

            mockedNativeFirmwareInstallerProxy.CancelCalled.Should().BeFalse();
            mockedNativeFirmwareInstallerProxy.DisconnectCalled.Should().BeFalse(); //00
            mockedNativeFirmwareInstallerProxy.BeginInstallationCalled.Should().BeTrue();

            eventsMonitor.OccurredEvents.Length.Should().Be(12);

            eventsMonitor
                .Should().Raise(nameof(firmwareInstaller.StateChanged))
                .WithSender(firmwareInstaller)
                .WithArgs<StateChangedEventArgs>(args => args.NewState == EFirmwareInstallationState.Uploading);
            
            eventsMonitor
                .Should()
                .Raise(nameof(firmwareInstaller.FirmwareUploadProgressPercentageAndDataThroughputChanged))
                .WithSender(firmwareInstaller);

            eventsMonitor
                .Should().Raise(nameof(firmwareInstaller.StateChanged))
                .WithSender(firmwareInstaller)
                .WithArgs<StateChangedEventArgs>(args => args.NewState == EFirmwareInstallationState.Complete);
            
            //00 we dont want to disconnect the device regardless of the outcome
        }

        private class MockedGreenNativeFirmwareInstallerProxySpy : MockedNativeFirmwareInstallerProxySpy
        {
            public MockedGreenNativeFirmwareInstallerProxySpy(INativeFirmwareInstallerCallbacksProxy firmwareInstallerCallbacksProxy)
                : base(firmwareInstallerCallbacksProxy)
            {
            }

            public override EFirmwareInstallationVerdict BeginInstallation(
                byte[] data,
                EFirmwareInstallationMode mode = EFirmwareInstallationMode.TestAndConfirm,
                bool? eraseSettings = null,
                int? estimatedSwapTimeInMilliseconds = null,
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
                    windowCapacity: windowCapacity,
                    memoryAlignment: memoryAlignment,
                    estimatedSwapTimeInMilliseconds: estimatedSwapTimeInMilliseconds
                );

                Task.Run(function: async () => //00 vital
                {
                    await Task.Delay(10);
                    
                    StateChangedAdvertisement(oldState: EFirmwareInstallationState.Idle, newState: EFirmwareInstallationState.Validating);
                    await Task.Delay(10);
                    
                    StateChangedAdvertisement(oldState: EFirmwareInstallationState.Validating, newState: EFirmwareInstallationState.Uploading);
                    await Task.Delay(100);
                    
                    FirmwareUploadProgressPercentageAndDataThroughputChangedAdvertisement(progressPercentage: 00, averageThroughput: 00);
                    await Task.Delay(10);
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