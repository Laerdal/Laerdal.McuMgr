using Laerdal.McuMgr.Common.Enums;
using Laerdal.McuMgr.FirmwareInstallation.Contracts.Enums;

namespace Laerdal.McuMgr.FirmwareInstallation.Contracts.Exceptions
{
    public sealed class FirmwareInstallationUnhealthyFirmwareDataGivenException : FirmwareInstallationErroredOutException
    {
        public FirmwareInstallationUnhealthyFirmwareDataGivenException( //@formatter:off
            string                            nativeErrorMessage = "",
            EFirmwareInstallerFatalErrorType  fatalErrorType     = EFirmwareInstallerFatalErrorType.GivenFirmwareDataUnhealthy, //managed-layer broad-error-code
            EGlobalErrorCode                  globalErrorCode    = EGlobalErrorCode.Generic //nordic-native-layer exacting error-code for better debugging and logs-reporting
        ) //@formatter:off
            : base(
                errorMessage: ProperlyFormatErrorMessage(nativeErrorMessage),
                fatalErrorType: fatalErrorType,
                globalErrorCode: globalErrorCode
            )
        {
        }

        static private string ProperlyFormatErrorMessage(string nativeErrorMessage)
        {
            const string prefix = "Firmware installation failed because the firmware-data passed to the installer were unhealthy (failed crc checks etc)";

            return string.IsNullOrWhiteSpace(nativeErrorMessage)
                ? prefix
                : $"{prefix}: {nativeErrorMessage}";
        }
    }
}
