using Laerdal.McuMgr.Common.Enums;

namespace Laerdal.McuMgr.FirmwareInstaller.Contracts.Exceptions
{
    public class FirmwareInstallationUploadingStageErroredOutException : FirmwareInstallationErroredOutException, IFirmwareInstallationException
    {
        public FirmwareInstallationUploadingStageErroredOutException(string internalErrorMessage, EGlobalErrorCode globalErrorCode)
            : base($"An error occurred while uploading the firmware: {internalErrorMessage}", globalErrorCode)
        {
        }
    }
}