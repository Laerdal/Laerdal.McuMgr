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
        [Theory]
        [InlineData("FIT.IA.STEIFCOTDD.GGNFDTSIT.010", 0, ECachedFirmwareType.CachedAndActive)]
        [InlineData("FIT.IA.STEIFCOTDD.GGNFDTSIT.020", 1, ECachedFirmwareType.CachedAndActive)]
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
            var firmwareInstaller = new McuMgr.FirmwareInstaller.FirmwareInstaller(mockedNativeFirmwareInstallerProxy);

            using var eventsMonitor = firmwareInstaller.Monitor();

            // Act
            var work = new Func<Task>(() => firmwareInstaller.InstallAsync(new byte[] { 1, 2, 3 }, maxRetriesCount: 0));

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
            )
                : base(firmwareInstallerCallbacksProxy)
            {
                _cachedFirmwareTypeToEmulate = cachedFirmwareTypeToEmulate;
                _numberOfFirmwareUploadingEventsToEmitCount = numberOfFirmwareUploadingEventsToEmitCount;
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

                    for (var i = 0; i < _numberOfFirmwareUploadingEventsToEmitCount; i++)
                    {
                        FirmwareUploadProgressPercentageAndDataThroughputChangedAdvertisement(progressPercentage: i + 1, averageThroughput: 10);
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

                            StateChangedAdvertisement(oldState: EFirmwareInstallationState.Testing, newState: EFirmwareInstallationState.Confirming);
                            await Task.Delay(10);

                            StateChangedAdvertisement(oldState: EFirmwareInstallationState.Confirming, newState: EFirmwareInstallationState.Resetting);
                            await Task.Delay(10);

                            StateChangedAdvertisement(oldState: EFirmwareInstallationState.Resetting, newState: EFirmwareInstallationState.Complete);
                            break;

                        default:
                            throw new ArgumentOutOfRangeException(nameof(_cachedFirmwareTypeToEmulate));
                    }
                });

                return verdict;

                //00 simulating the state changes in a background thread is vital in order to simulate the async nature of the native uploader
            }
        }
    }
}