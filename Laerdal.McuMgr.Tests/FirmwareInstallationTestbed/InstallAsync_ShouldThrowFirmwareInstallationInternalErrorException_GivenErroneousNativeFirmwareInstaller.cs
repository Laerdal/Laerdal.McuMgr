using FluentAssertions;
using FluentAssertions.Extensions;
using Laerdal.McuMgr.FirmwareInstallation;
using Laerdal.McuMgr.FirmwareInstallation.Contracts.Enums;
using Laerdal.McuMgr.FirmwareInstallation.Contracts.Exceptions;
using Laerdal.McuMgr.FirmwareInstallation.Contracts.Native;
using GenericNativeFirmwareInstallerCallbacksProxy_ = Laerdal.McuMgr.FirmwareInstallation.FirmwareInstaller.GenericNativeFirmwareInstallerCallbacksProxy;

namespace Laerdal.McuMgr.Tests.FirmwareInstallationTestbed
{
    public partial class FirmwareInstallerTestbed
    {
        [Fact]
        public async Task InstallAsync_ShouldThrowFirmwareInstallationInternalErrorException_GivenErroneousNativeFirmwareInstaller()
        {
            // Arrange
            var mockedNativeFirmwareInstallerProxy = new MockedErroneousNativeFirmwareInstallerProxySpy(new GenericNativeFirmwareInstallerCallbacksProxy_());
            var firmwareInstaller = new FirmwareInstaller(mockedNativeFirmwareInstallerProxy);

            // Act
            var work = new Func<Task>(() => firmwareInstaller.InstallAsync(
                data: [1],
                maxTriesCount: 1,
                hostDeviceModel: "foobar",
                hostDeviceManufacturer: "acme corp."
            ));

            // Assert
            (
                await work.Should().ThrowWithinAsync<FirmwareInstallationInternalErrorException>(1_000.Milliseconds())
            ).WithInnerExceptionExactly<Exception>("native symbols not loaded blah blah");

            mockedNativeFirmwareInstallerProxy.CancelCalled.Should().BeFalse();
            mockedNativeFirmwareInstallerProxy.DisconnectCalled.Should().BeFalse(); //00
            mockedNativeFirmwareInstallerProxy.BeginInstallationCalled.Should().BeTrue();

            //00 we dont want to disconnect the device regardless of the outcome
        }

        private class MockedErroneousNativeFirmwareInstallerProxySpy : MockedNativeFirmwareInstallerProxySpy
        {
            public MockedErroneousNativeFirmwareInstallerProxySpy(INativeFirmwareInstallerCallbacksProxy firmwareInstallerCallbacksProxy) : base(firmwareInstallerCallbacksProxy)
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
                base.BeginInstallation(
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

                Thread.Sleep(100);

                throw new Exception("native symbols not loaded blah blah");
            }
        }
    }
}