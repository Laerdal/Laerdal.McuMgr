// ReSharper disable UnusedParameter.Global

using Laerdal.McuMgr.FirmwareInstallation.Contracts.Enums;

namespace Laerdal.McuMgr.FirmwareInstallation.Contracts.Native
{
    public interface INativeFirmwareInstallerCommandableProxy
    {
        void Cancel();
        void Disconnect();

        EFirmwareInstallationVerdict NativeBeginInstallation(byte[] data,
            EFirmwareInstallationMode mode = EFirmwareInstallationMode.TestAndConfirm,
            bool? eraseSettings = null,
            int? estimatedSwapTimeInMilliseconds = null,
            int? initialMtuSize = null,
            int? windowCapacity = null, //   android only    not applicable for ios
            int? memoryAlignment = null, //  android only    not applicable for ios
            int? pipelineDepth = null, //    ios only        not applicable for android
            int? byteAlignment = null

            //    ios only        not applicable for android
        );
    }
}