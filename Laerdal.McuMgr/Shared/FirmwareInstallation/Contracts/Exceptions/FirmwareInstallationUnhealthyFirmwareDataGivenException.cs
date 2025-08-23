using Laerdal.McuMgr.Common.Enums;
using Laerdal.McuMgr.FirmwareInstallation.Contracts.Enums;

namespace Laerdal.McuMgr.FirmwareInstallation.Contracts.Exceptions
{
    public sealed class FirmwareInstallationUnhealthyFirmwareDataGivenException : FirmwareInstallationErroredOutException
    {
        public FirmwareInstallationUnhealthyFirmwareDataGivenException(string nativeErrorMessage, EFirmwareInstallerFatalErrorType fatalErrorType, EGlobalErrorCode eaGlobalErrorCode)
            : base(
                errorMessage: $"Firmware given was found to be unhealthy and has not been installed: {nativeErrorMessage}",
                fatalErrorType: fatalErrorType,
                globalErrorCode: eaGlobalErrorCode
            )
        {
        }
    }
}