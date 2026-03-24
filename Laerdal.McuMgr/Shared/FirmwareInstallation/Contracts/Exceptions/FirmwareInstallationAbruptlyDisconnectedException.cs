using Laerdal.McuMgr.Common.Enums;
using Laerdal.McuMgr.FirmwareInstallation.Contracts.Enums;

namespace Laerdal.McuMgr.FirmwareInstallation.Contracts.Exceptions
{
    public sealed class FirmwareInstallationAbruptlyDisconnectedException : FirmwareInstallationUploadingStageErroredOutException
    {
        public FirmwareInstallationAbruptlyDisconnectedException( //@formatter:off
            string             nativeErrorMessage = "",
            EGlobalErrorCode   globalErrorCode    = EGlobalErrorCode.SubSystemMcuMgrTransport_Disconnected //nordic-native-layer exacting error-code for better debugging and logs-reporting
        ) //@formatter:off
            : base(
                fatalErrorType: EFirmwareInstallerFatalErrorType.DeviceDisconnectedAbruptly,
                globalErrorCode: globalErrorCode,
                internalErrorMessage: ProperlyFormatErrorMessage(nativeErrorMessage)
            )
        {
        }

        static private string ProperlyFormatErrorMessage(string nativeErrorMessage)
        {
            const string prefix = "Firmware installation failed because the remote device disconnected abruptly";

            return string.IsNullOrWhiteSpace(nativeErrorMessage)
                ? prefix
                : $"{prefix}: {nativeErrorMessage}";
        }
    }
}