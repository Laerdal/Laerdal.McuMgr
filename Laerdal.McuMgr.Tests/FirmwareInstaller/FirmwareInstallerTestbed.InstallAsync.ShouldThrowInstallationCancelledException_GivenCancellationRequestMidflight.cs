using System.Diagnostics.CodeAnalysis;
using FluentAssertions;
using FluentAssertions.Extensions;
using Laerdal.McuMgr.Common.Helpers;
using Laerdal.McuMgr.FirmwareInstaller.Contracts;
using Laerdal.McuMgr.FirmwareInstaller.Contracts.Enums;
using Laerdal.McuMgr.FirmwareInstaller.Contracts.Events;
using Laerdal.McuMgr.FirmwareInstaller.Contracts.Exceptions;
using Laerdal.McuMgr.FirmwareInstaller.Contracts.Native;
using GenericNativeFirmwareInstallerCallbacksProxy_ = Laerdal.McuMgr.FirmwareInstaller.FirmwareInstaller.GenericNativeFirmwareInstallerCallbacksProxy;

namespace Laerdal.McuMgr.Tests.FirmwareInstaller
{
    [SuppressMessage("Usage", "xUnit1026:Theory methods should use all of their parameters")]
    public partial class FirmwareInstallerTestbed
    {
        [Theory]
        [InlineData("FIT.IA.STFICE.GCRM.010", true)]
        [InlineData("FIT.IA.STFICE.GCRM.020", false)]
        public async Task InstallAsync_ShouldThrowFirmwareInstallationCancelledException_GivenCancellationRequestMidflight(string testcaseNickname, bool isCancellationLeadingToSoftLanding)
        {
            // Arrange
            var mockedNativeFirmwareInstallerProxy = new MockedGreenNativeFirmwareInstallerProxySpy3(new GenericNativeFirmwareInstallerCallbacksProxy_(), isCancellationLeadingToSoftLanding);
            var firmwareInstaller = new McuMgr.FirmwareInstaller.FirmwareInstaller(mockedNativeFirmwareInstallerProxy);

            using var eventsMonitor = firmwareInstaller.Monitor();

            // Act
            _ = Task.Run(async () =>
            {
                await Task.Delay(500);

                firmwareInstaller.Cancel();
            });
            var work = new Func<Task>(() => firmwareInstaller.InstallAsync(new byte[] { 1, 2, 3 }, maxTriesCount: 1));

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
            private readonly bool _isCancellationLeadingToSoftLanding;
            private CancellationTokenSource _cancellationTokenSource;
            
            public MockedGreenNativeFirmwareInstallerProxySpy3(INativeFirmwareInstallerCallbacksProxy firmwareInstallerCallbacksProxy, bool isCancellationLeadingToSoftLanding) : base(firmwareInstallerCallbacksProxy)
            {
                _isCancellationLeadingToSoftLanding = isCancellationLeadingToSoftLanding;
            }
            
            public override void Cancel()
            {
                base.Cancel();

                Task.Run(async () => // under normal circumstances the native implementation will bubble up these events in this exact order
                {
                    await Task.Delay(100);
                    StateChangedAdvertisement(oldState: EFirmwareInstallationState.Uploading, newState: EFirmwareInstallationState.Cancelling);
                    
                    await Task.Delay(100);
                    if (_isCancellationLeadingToSoftLanding)
                    {
                        StateChangedAdvertisement(oldState: EFirmwareInstallationState.Cancelling, newState: EFirmwareInstallationState.Cancelled); //   order
                        CancelledAdvertisement(); //                                                                                                     order    
                    }
                });
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
                
                (FirmwareInstaller as IFirmwareInstallerEventSubscribable)!.Cancelled += (_, _) =>
                {
                    _cancellationTokenSource.Cancel();
                };

                _cancellationTokenSource = new CancellationTokenSource();

                Task.Run(async () => //00 vital
                {
                    await Task.Delay(100, _cancellationTokenSource.Token);
                    if (_cancellationTokenSource.IsCancellationRequested)
                        return;
                    
                    await Task.Delay(100, _cancellationTokenSource.Token);
                    StateChangedAdvertisement(EFirmwareInstallationState.Idle, EFirmwareInstallationState.Idle);
                    
                    await Task.Delay(100, _cancellationTokenSource.Token);
                    StateChangedAdvertisement(EFirmwareInstallationState.Idle, EFirmwareInstallationState.Validating);
                    
                    await Task.Delay(100, _cancellationTokenSource.Token);
                    StateChangedAdvertisement(EFirmwareInstallationState.Validating, EFirmwareInstallationState.Uploading);

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