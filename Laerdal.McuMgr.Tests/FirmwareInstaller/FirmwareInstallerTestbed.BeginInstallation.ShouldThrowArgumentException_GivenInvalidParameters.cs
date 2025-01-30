using FluentAssertions;
using Laerdal.McuMgr.FirmwareInstaller.Contracts.Enums;
using Laerdal.McuMgr.FirmwareInstaller.Contracts.Native;
using GenericNativeFirmwareInstallerCallbacksProxy_ = Laerdal.McuMgr.FirmwareInstaller.FirmwareInstaller.GenericNativeFirmwareInstallerCallbacksProxy;

#pragma warning disable xUnit1026

namespace Laerdal.McuMgr.Tests.FirmwareInstaller
{
    public partial class FirmwareInstallerTestbed
    {
        [Theory]
        [InlineData("FIT.BI.STAE.GIFDB.010", new byte[] {}, "foobar", "acme corp.")]
        [InlineData("FIT.BI.STAE.GIFDB.020", null, "foobar", "acme corp.")]
        [InlineData("FIT.BI.STAE.GIFDB.030", new byte[] { 1 }, "", "acme corp.")] //        invalid hostDeviceModel
        [InlineData("FIT.BI.STAE.GIFDB.040", new byte[] { 1 }, "  ", "acme corp.")] //      invalid hostDeviceModel
        [InlineData("FIT.BI.STAE.GIFDB.050", new byte[] { 1 }, "foobar", "")] //            invalid hostDeviceManufacturer
        [InlineData("FIT.BI.STAE.GIFDB.060", new byte[] { 1 }, "foobar", "  ")] //          invalid hostDeviceManufacturer
        public void BeginInstallation_ShouldThrowArgumentException_GivenInvalidParameters(string testcaseNickname, byte[] mockedFileData, string hostDeviceModel, string hostDeviceManufacturer)
        {
            // Arrange
            var mockedNativeFirmwareInstallerProxy = new MockedGreenNativeFirmwareInstallerProxySpy1(new GenericNativeFirmwareInstallerCallbacksProxy_());
            var firmwareInstaller = new McuMgr.FirmwareInstaller.FirmwareInstaller(mockedNativeFirmwareInstallerProxy);

            using var eventsMonitor = firmwareInstaller.Monitor();

            // Act
            var work = new Func<EFirmwareInstallationVerdict>(() => firmwareInstaller.BeginInstallation(
                data: mockedFileData,
                hostDeviceModel: hostDeviceModel,
                hostDeviceManufacturer: hostDeviceManufacturer
            ));

            // Assert
            work.Should().Throw<ArgumentException>();

            mockedNativeFirmwareInstallerProxy.CancelCalled.Should().BeFalse();
            mockedNativeFirmwareInstallerProxy.DisconnectCalled.Should().BeFalse(); //00
            mockedNativeFirmwareInstallerProxy.BeginInstallationCalled.Should().BeFalse();

            eventsMonitor.Should().NotRaise(nameof(firmwareInstaller.StateChanged));

            //00 we dont want to disconnect the device regardless of the outcome
        }
        
        private class MockedGreenNativeFirmwareInstallerProxySpy1 : MockedNativeFirmwareInstallerProxySpy
        {
            public MockedGreenNativeFirmwareInstallerProxySpy1(INativeFirmwareInstallerCallbacksProxy firmwareInstallerCallbacksProxy) : base(firmwareInstallerCallbacksProxy)
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
                
                Task.Run(async () => //00 vital
                {
                    await Task.Delay(10);
                    StateChangedAdvertisement(EFirmwareInstallationState.Idle, EFirmwareInstallationState.Idle);
                    
                    await Task.Delay(10);
                    StateChangedAdvertisement(EFirmwareInstallationState.Idle, EFirmwareInstallationState.Uploading);

                    await Task.Delay(20);
                    
                    StateChangedAdvertisement(EFirmwareInstallationState.Uploading, EFirmwareInstallationState.Complete);
                });
                
                return verdict;
                
                //00 simulating the state changes in a background thread is vital in order to simulate the async nature of the native uploader
            }
        }
    }
}