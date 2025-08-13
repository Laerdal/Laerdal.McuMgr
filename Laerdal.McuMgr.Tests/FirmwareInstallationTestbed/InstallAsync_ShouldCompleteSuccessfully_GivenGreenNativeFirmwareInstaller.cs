using FluentAssertions;
using FluentAssertions.Extensions;
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
        public async Task InstallAsync_ShouldCompleteSuccessfully_GivenGreenNativeFirmwareInstaller()
        {
            // Arrange
            var mockedNativeFirmwareInstallerProxy = new MockedGreenNativeFirmwareInstallerProxySpy(new GenericNativeFirmwareInstallerCallbacksProxy_());
            var firmwareInstaller = new FirmwareInstaller(mockedNativeFirmwareInstallerProxy);

            using var eventsMonitor = firmwareInstaller.Monitor();

            // Act
            var work = new Func<Task>(() => firmwareInstaller.InstallAsync(
                data: [1, 2, 3],
                maxTriesCount: 1,
                hostDeviceModel: "foobar",
                hostDeviceManufacturer: "acme corp."
            ));

            // Assert
            await work.Should().CompleteWithinAsync(4.Seconds());

            mockedNativeFirmwareInstallerProxy.CancelCalled.Should().BeFalse();
            mockedNativeFirmwareInstallerProxy.DisconnectCalled.Should().BeFalse(); //00
            mockedNativeFirmwareInstallerProxy.BeginInstallationCalled.Should().BeTrue();

            eventsMonitor.OccurredEvents.Length.Should().Be(26);

            eventsMonitor
                .Should()
                .NotRaise(nameof(firmwareInstaller.IdenticalFirmwareCachedOnTargetDeviceDetected));
            
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

            var overallProgressPercentages = eventsMonitor.OccurredEvents
                .Where(args => args.EventName == nameof(firmwareInstaller.OverallProgressPercentageChanged))
                .SelectMany(x => x.Parameters)
                .OfType<OverallProgressPercentageChangedEventArgs>()
                .Select(x => x.ProgressPercentage)
                .ToArray();
            
            overallProgressPercentages.Min().Should().Be(0);
            overallProgressPercentages.Max().Should().Be(100);
            overallProgressPercentages.Length.Should().Be(12);
            overallProgressPercentages.Should().BeInAscendingOrder();

            overallProgressPercentages.Should().ContainInOrder( //@formatter:off
                FirmwareInstaller.GetProgressMilestonePercentageForState(EFirmwareInstallationState.None       )!.Value,
                FirmwareInstaller.GetProgressMilestonePercentageForState(EFirmwareInstallationState.Idle       )!.Value,
                FirmwareInstaller.GetProgressMilestonePercentageForState(EFirmwareInstallationState.Validating )!.Value,
                FirmwareInstaller.GetProgressMilestonePercentageForState(EFirmwareInstallationState.Uploading  )!.Value,
                FirmwareInstaller.GetProgressMilestonePercentageForState(EFirmwareInstallationState.Testing    )!.Value,
                FirmwareInstaller.GetProgressMilestonePercentageForState(EFirmwareInstallationState.Resetting  )!.Value,
                FirmwareInstaller.GetProgressMilestonePercentageForState(EFirmwareInstallationState.Confirming )!.Value,
                FirmwareInstaller.GetProgressMilestonePercentageForState(EFirmwareInstallationState.Complete   )!.Value
            ); //@formatter:on

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
                    
                    FirmwareUploadProgressPercentageAndDataThroughputChangedAdvertisement(progressPercentage: 00, currentThroughputInKbps: 00, totalAverageThroughputInKbps: 00);
                    await Task.Delay(10);
                    FirmwareUploadProgressPercentageAndDataThroughputChangedAdvertisement(progressPercentage: 20, currentThroughputInKbps: 10, totalAverageThroughputInKbps: 10);
                    await Task.Delay(10);
                    FirmwareUploadProgressPercentageAndDataThroughputChangedAdvertisement(progressPercentage: 40, currentThroughputInKbps: 10, totalAverageThroughputInKbps: 10);
                    await Task.Delay(10);
                    FirmwareUploadProgressPercentageAndDataThroughputChangedAdvertisement(progressPercentage: 60, currentThroughputInKbps: 10, totalAverageThroughputInKbps: 10);
                    await Task.Delay(10);
                    FirmwareUploadProgressPercentageAndDataThroughputChangedAdvertisement(progressPercentage: 80, currentThroughputInKbps: 10, totalAverageThroughputInKbps: 10);
                    await Task.Delay(10);
                    FirmwareUploadProgressPercentageAndDataThroughputChangedAdvertisement(progressPercentage: 100, currentThroughputInKbps: 10, totalAverageThroughputInKbps: 10);
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