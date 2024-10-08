// ReSharper disable UnusedParameter.Global

using Laerdal.McuMgr.FirmwareInstaller.Contracts.Enums;

namespace Laerdal.McuMgr.FirmwareInstaller.Contracts.Native
{
    internal interface INativeFirmwareInstallerCommandableProxy
    {
        void Cancel();
        void Disconnect();

        EFirmwareInstallationVerdict BeginInstallation(
            byte[] data,
            EFirmwareInstallationMode mode = EFirmwareInstallationMode.TestAndConfirm,
            bool? eraseSettings = null,
            int? estimatedSwapTimeInMilliseconds = null,
            int? initialMtuSize = null, //   android only    not applicable for ios
            int? windowCapacity = null, //   android only    not applicable for ios
            int? memoryAlignment = null, //  android only    not applicable for ios
            int? pipelineDepth = null, //    ios only        not applicable for android
            int? byteAlignment = null //     ios only        not applicable for android
        );
    }
}