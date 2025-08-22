using Laerdal.McuMgr.Common.Enums;
using Laerdal.McuMgr.FirmwareInstallation.Contracts.Enums;

namespace Laerdal.McuMgr.FirmwareInstallation.Contracts.Exceptions
{
    public sealed class FirmwareInstallationUploadingStageErroredOutException : FirmwareInstallationErroredOutException
    {
        public FirmwareInstallationUploadingStageErroredOutException(string internalErrorMessage, EFirmwareInstallerFatalErrorType fatalErrorType, EGlobalErrorCode globalErrorCode)
            : base($"An error occurred while uploading the firmware: {internalErrorMessage}", fatalErrorType, globalErrorCode)
        {
        }
    }
}