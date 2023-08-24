using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Extensions;
using Laerdal.McuMgr.Common;
using Laerdal.McuMgr.FirmwareInstaller.Contracts.Enums;
using Laerdal.McuMgr.FirmwareInstaller.Contracts.Events;
using Laerdal.McuMgr.FirmwareInstaller.Contracts.Exceptions;
using Laerdal.McuMgr.FirmwareInstaller.Contracts.Native;
using Xunit;
using GenericNativeFirmwareInstallerCallbacksProxy_ = Laerdal.McuMgr.FirmwareInstaller.FirmwareInstaller.GenericNativeFirmwareInstallerCallbacksProxy;

namespace Laerdal.McuMgr.Tests.FirmwareInstaller
{
    [SuppressMessage("Usage", "xUnit1026:Theory methods should use all of their parameters")]
    public partial class FirmwareInstallerTestbed
    {
        [Theory]
        [InlineData("FDT.IA.STFIEOISTE.GDEM.010", "")] //    if the firmware swapping times out then we get a
        [InlineData("FDT.IA.STFIEOISTE.GDEM.020", null)] //  dud error message from the underlying native device
        public async Task InstallAsync_ShouldThrowFirmwareInstallationErroredOutImageSwapTimeoutException_GivenDudErrorMessage(string testcaseNickname, string nativeRogueErrorMessage)
        {
            // Arrange
            var mockedNativeFirmwareInstallerProxy = new MockedErroneousNativeFirmwareInstallerProxySpy13(
                nativeErrorMessage: nativeRogueErrorMessage,
                firmwareInstallerCallbacksProxy: new GenericNativeFirmwareInstallerCallbacksProxy_()
            );
            var firmwareInstaller = new McuMgr.FirmwareInstaller.FirmwareInstaller(mockedNativeFirmwareInstallerProxy);

            using var eventsMonitor = firmwareInstaller.Monitor();

            // Act
            var work = new Func<Task>(() => firmwareInstaller.InstallAsync(data: new byte[] { 1, 2, 3 }));

            // Assert
            await work.Should()
                .ThrowExactlyAsync<FirmwareInstallationErroredOutImageSwapTimeoutException>()
                .WithTimeoutInMs((int)3.Seconds().TotalMilliseconds);

            mockedNativeFirmwareInstallerProxy.CancelCalled.Should().BeFalse();
            mockedNativeFirmwareInstallerProxy.DisconnectCalled.Should().BeFalse(); //00
            mockedNativeFirmwareInstallerProxy.BeginInstallationCalled.Should().BeTrue();

            eventsMonitor.Should().NotRaise(nameof(firmwareInstaller.Cancelled));

            eventsMonitor.OccurredEvents
                .Count(x => x.EventName == nameof(firmwareInstaller.FatalErrorOccurred))
                .Should().Be(1);

            eventsMonitor
                .Should().Raise(nameof(firmwareInstaller.FatalErrorOccurred))
                .WithSender(firmwareInstaller);

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

        private class MockedErroneousNativeFirmwareInstallerProxySpy13 : MockedNativeFirmwareInstallerProxySpy
        {
            private readonly string _nativeErrorMessage;
            
            public MockedErroneousNativeFirmwareInstallerProxySpy13(INativeFirmwareInstallerCallbacksProxy firmwareInstallerCallbacksProxy, string nativeErrorMessage) : base(firmwareInstallerCallbacksProxy)
            {
                _nativeErrorMessage = nativeErrorMessage;
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
                    await Task.Delay(100);

                    StateChangedAdvertisement(EFirmwareInstallationState.Idle, EFirmwareInstallationState.Validating);
                    await Task.Delay(100);
                    
                    StateChangedAdvertisement(EFirmwareInstallationState.Validating, EFirmwareInstallationState.Uploading);
                    await Task.Delay(100);
                    
                    StateChangedAdvertisement(EFirmwareInstallationState.Uploading, EFirmwareInstallationState.Testing);
                    await Task.Delay(100);
                    
                    FatalErrorOccurredAdvertisement(_nativeErrorMessage);
                    StateChangedAdvertisement(EFirmwareInstallationState.Uploading, EFirmwareInstallationState.Error);
                });

                return verdict;

                //00 simulating the state changes in a background thread is vital in order to simulate the async nature of the native uploader
            }
        }
    }
}