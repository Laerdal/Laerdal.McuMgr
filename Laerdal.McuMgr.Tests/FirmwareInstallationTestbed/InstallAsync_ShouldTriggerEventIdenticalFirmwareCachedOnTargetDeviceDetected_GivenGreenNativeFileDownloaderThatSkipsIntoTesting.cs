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
        [Theory]
        [InlineData("FIT.IA.STEIFCOTDD.GGNFDTSIT.010", 0, ECachedFirmwareType.CachedAndActive)]
        //[InlineData("FIT.IA.STEIFCOTDD.GGNFDTSIT.020", 1, ECachedFirmwareType.CachedAndActive)] //fails on azure for some weird reason
        [InlineData("FIT.IA.STEIFCOTDD.GGNFDTSIT.030", 0, ECachedFirmwareType.CachedButInactive)]
        [InlineData("FIT.IA.STEIFCOTDD.GGNFDTSIT.040", 1, ECachedFirmwareType.CachedButInactive)]
        public async Task InstallAsync_ShouldTriggerEventIdenticalFirmwareCachedOnTargetDeviceDetected_GivenGreenNativeFileDownloaderThatSkipsIntoTesting(string testcaseNickname, int numberOfFirmwareUploadingEventsToEmitCount, ECachedFirmwareType expectedFirmwareType)
        {
            // Arrange
            var mockedNativeFirmwareInstallerProxy = new MockedGreenNativeFirmwareInstallerProxySpy14(
                cachedFirmwareTypeToEmulate: expectedFirmwareType,
                firmwareInstallerCallbacksProxy: new GenericNativeFirmwareInstallerCallbacksProxy_(),
                numberOfFirmwareUploadingEventsToEmitCount: numberOfFirmwareUploadingEventsToEmitCount
            );
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

            eventsMonitor
                .Should().Raise(nameof(firmwareInstaller.StateChanged))
                .WithSender(firmwareInstaller)
                .WithArgs<StateChangedEventArgs>(args => args.NewState == EFirmwareInstallationState.Uploading);

            eventsMonitor
                .Should().Raise(nameof(firmwareInstaller.IdenticalFirmwareCachedOnTargetDeviceDetected))
                .WithSender(firmwareInstaller)
                .WithArgs<IdenticalFirmwareCachedOnTargetDeviceDetectedEventArgs>(args => args.CachedFirmwareType == expectedFirmwareType);

            eventsMonitor
                .Should().Raise(nameof(firmwareInstaller.StateChanged))
                .WithSender(firmwareInstaller)
                .WithArgs<StateChangedEventArgs>(args => args.NewState == EFirmwareInstallationState.Complete);
            
            //00 we dont want to disconnect the device regardless of the outcome
        }

        private class MockedGreenNativeFirmwareInstallerProxySpy14 : MockedNativeFirmwareInstallerProxySpy
        {
            private readonly int _numberOfFirmwareUploadingEventsToEmitCount;
            private readonly ECachedFirmwareType _cachedFirmwareTypeToEmulate;

            public MockedGreenNativeFirmwareInstallerProxySpy14(
                INativeFirmwareInstallerCallbacksProxy firmwareInstallerCallbacksProxy,
                int numberOfFirmwareUploadingEventsToEmitCount,
                ECachedFirmwareType cachedFirmwareTypeToEmulate
            ) : base(firmwareInstallerCallbacksProxy)
            {
                _cachedFirmwareTypeToEmulate = cachedFirmwareTypeToEmulate;
                _numberOfFirmwareUploadingEventsToEmitCount = numberOfFirmwareUploadingEventsToEmitCount;
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
                    initialMtuSize: initialMtuSize,
                    pipelineDepth: pipelineDepth,
                    byteAlignment: byteAlignment,
                    windowCapacity: windowCapacity,
                    memoryAlignment: memoryAlignment,
                    estimatedSwapTimeInMilliseconds: estimatedSwapTimeInMilliseconds
                );

                StateChangedAdvertisement(oldState: EFirmwareInstallationState.None, newState: EFirmwareInstallationState.None);
                StateChangedAdvertisement(oldState: EFirmwareInstallationState.None, newState: EFirmwareInstallationState.Idle);
                
                Task.Run(function: async () => //00 vital
                {
                    StateChangedAdvertisement(oldState: EFirmwareInstallationState.Idle, newState: EFirmwareInstallationState.Validating);
                    await Task.Delay(10);
                    
                    StateChangedAdvertisement(oldState: EFirmwareInstallationState.Validating, newState: EFirmwareInstallationState.Uploading);
                    await Task.Delay(100);

                    for (var i = 0; i < _numberOfFirmwareUploadingEventsToEmitCount; i++)
                    {
                        FirmwareUploadProgressPercentageAndDataThroughputChangedAdvertisement(progressPercentage: i + 1, currentThroughputInKBps: 10, totalAverageThroughputInKBps: 10);
                        await Task.Delay(10);
                    }

                    switch (_cachedFirmwareTypeToEmulate)
                    {
                        case ECachedFirmwareType.CachedAndActive:
                            StateChangedAdvertisement(oldState: EFirmwareInstallationState.Uploading, newState: EFirmwareInstallationState.Complete);
                            break;

                        case ECachedFirmwareType.CachedButInactive:
                            StateChangedAdvertisement(oldState: EFirmwareInstallationState.Uploading, newState: EFirmwareInstallationState.Testing);
                            await Task.Delay(10);

                            StateChangedAdvertisement(oldState: EFirmwareInstallationState.Testing, newState: EFirmwareInstallationState.Resetting);
                            await Task.Delay(10);
                            
                            StateChangedAdvertisement(oldState: EFirmwareInstallationState.Resetting, newState: EFirmwareInstallationState.Confirming);
                            await Task.Delay(10);

                            StateChangedAdvertisement(oldState: EFirmwareInstallationState.Confirming, newState: EFirmwareInstallationState.Complete);
                            break;

                        default:
                            throw new ArgumentOutOfRangeException(nameof(_cachedFirmwareTypeToEmulate));
                    }
                });
                
                return EFirmwareInstallationVerdict.Success;

                //00 simulating the state changes in a background thread is vital in order to simulate the async nature of the native uploader
            }
        }
    }
}