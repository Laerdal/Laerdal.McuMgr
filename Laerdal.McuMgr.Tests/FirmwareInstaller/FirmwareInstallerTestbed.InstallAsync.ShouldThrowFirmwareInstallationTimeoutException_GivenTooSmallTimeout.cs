using System;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Extensions;
using Laerdal.McuMgr.Common.Helpers;
using Laerdal.McuMgr.FirmwareInstaller.Contracts.Enums;
using Laerdal.McuMgr.FirmwareInstaller.Contracts.Events;
using Laerdal.McuMgr.FirmwareInstaller.Contracts.Exceptions;
using Laerdal.McuMgr.FirmwareInstaller.Contracts.Native;
using Xunit;
using GenericNativeFirmwareInstallerCallbacksProxy_ = Laerdal.McuMgr.FirmwareInstaller.FirmwareInstaller.GenericNativeFirmwareInstallerCallbacksProxy;

namespace Laerdal.McuMgr.Tests.FirmwareInstaller
{
    public partial class FirmwareInstallerTestbed
    {
        [Fact]
        public async Task InstallAsync_ShouldThrowFirmwareInstallationTimeoutException_GivenTooSmallTimeout()
        {
            // Arrange
            var mockedNativeFirmwareInstallerProxy = new MockedGreenButSlowNativeFirmwareInstallerProxySpy(new GenericNativeFirmwareInstallerCallbacksProxy_());
            var firmwareInstaller = new McuMgr.FirmwareInstaller.FirmwareInstaller(mockedNativeFirmwareInstallerProxy);

            using var eventsMonitor = firmwareInstaller.Monitor();

            // Act
            var work = new Func<Task>(() => firmwareInstaller.InstallAsync(new byte[] { 1 }, maxRetriesCount: 0, timeoutInMs: 100));

            // Assert
            await work.Should()
                .ThrowExactlyAsync<FirmwareInstallationTimeoutException>()
                .WithTimeoutInMs((int)2.Seconds().TotalMilliseconds);

            mockedNativeFirmwareInstallerProxy.CancelCalled.Should().BeFalse();
            mockedNativeFirmwareInstallerProxy.DisconnectCalled.Should().BeFalse(); //00
            mockedNativeFirmwareInstallerProxy.BeginInstallationCalled.Should().BeTrue();

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

        private class MockedGreenButSlowNativeFirmwareInstallerProxySpy : MockedNativeFirmwareInstallerProxySpy
        {
            public MockedGreenButSlowNativeFirmwareInstallerProxySpy(INativeFirmwareInstallerCallbacksProxy resetterCallbacksProxy) : base(resetterCallbacksProxy)
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

                Task.Run(async () => //00 vital
                {
                    await Task.Delay(10);
                    StateChangedAdvertisement(oldState: EFirmwareInstallationState.Idle, newState: EFirmwareInstallationState.Uploading);

                    await Task.Delay(1_000);
                    StateChangedAdvertisement(oldState: EFirmwareInstallationState.Uploading, newState: EFirmwareInstallationState.Complete);
                });

                return verdict;

                //00 simulating the state changes in a background thread is vital in order to simulate the async nature of the native resetter
            }
        }
    }
}