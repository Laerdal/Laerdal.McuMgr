using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Extensions;
using Laerdal.McuMgr.Common;
using Laerdal.McuMgr.FirmwareInstaller.Contracts;
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
        public async Task InstallAsync_ShouldThrowFirmwareInstallationCancelledException_GivenCancellationRequestMidflight()
        {
            // Arrange
            var mockedNativeFirmwareInstallerProxy = new MockedGreenNativeFirmwareInstallerProxySpy3(new GenericNativeFirmwareInstallerCallbacksProxy_());
            var firmwareInstaller = new McuMgr.FirmwareInstaller.FirmwareInstaller(mockedNativeFirmwareInstallerProxy);

            using var eventsMonitor = firmwareInstaller.Monitor();

            // Act
            _ = Task.Run(async () =>
            {
                await Task.Delay(500).ConfigureAwait(false);

                firmwareInstaller.Cancel();
            });
            var work = new Func<Task>(() => firmwareInstaller.InstallAsync(new byte[] { 1, 2, 3 }));

            // Assert
            await work.Should()
                .ThrowExactlyAsync<FirmwareInstallationCancelledException>()
                .WithTimeoutInMs((int)5.Seconds().TotalMilliseconds);

            mockedNativeFirmwareInstallerProxy.CancelCalled.Should().BeTrue();
            mockedNativeFirmwareInstallerProxy.DisconnectCalled.Should().BeFalse(); //00
            mockedNativeFirmwareInstallerProxy.BeginInstallationCalled.Should().BeTrue();

            eventsMonitor.Should().Raise(nameof(firmwareInstaller.Cancelled));
            
            eventsMonitor
                .Should().Raise(nameof(firmwareInstaller.StateChanged))
                .WithSender(firmwareInstaller)
                .WithArgs<StateChangedEventArgs>(args => args.NewState == EFirmwareInstallationState.Uploading);

            //00 we dont want to disconnect the device regardless of the outcome
        }

        private class MockedGreenNativeFirmwareInstallerProxySpy3 : MockedNativeFirmwareInstallerProxySpy
        {
            private CancellationTokenSource _cancellationTokenSource;
            
            public MockedGreenNativeFirmwareInstallerProxySpy3(INativeFirmwareInstallerCallbacksProxy firmwareInstallerCallbacksProxy) : base(firmwareInstallerCallbacksProxy)
            {
            }
            
            public override void Cancel()
            {
                base.Cancel();

                Task.Run(async () => // under normal circumstances the native implementation will bubble up these events in this exact order
                {
                    await Task.Delay(100);
                    StateChangedAdvertisement(oldState: EFirmwareInstallationState.Idle, newState: EFirmwareInstallationState.Cancelled); //   order
                    CancelledAdvertisement(); //                                                                                               order
                });
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
                
                (FirmwareInstaller as IFirmwareInstallerEventSubscribable)!.Cancelled += (sender, args) =>
                {
                    _cancellationTokenSource.Cancel();
                };

                _cancellationTokenSource = new CancellationTokenSource();

                Task.Run(async () => //00 vital
                {
                    await Task.Delay(100, _cancellationTokenSource.Token);
                    if (_cancellationTokenSource.IsCancellationRequested)
                        return;

                    StateChangedAdvertisement(EFirmwareInstallationState.Idle, EFirmwareInstallationState.Uploading);

                    await Task.Delay(20_000, _cancellationTokenSource.Token);
                    if (_cancellationTokenSource.IsCancellationRequested)
                        return;

                    StateChangedAdvertisement(EFirmwareInstallationState.Uploading, EFirmwareInstallationState.Complete);
                }, _cancellationTokenSource.Token);

                return verdict;

                //00 simulating the state changes in a background thread is vital in order to simulate the async nature of the native uploader
            }
        }
    }
}