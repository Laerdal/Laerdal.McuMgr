namespace Laerdal.McuMgr.FirmwareInstaller.Contracts.Exceptions
{
    public class FirmwareInstallationUploadingStageErroredOutException : FirmwareInstallationErroredOutException, IFirmwareInstallationException
    {
        public FirmwareInstallationUploadingStageErroredOutException()
            : base("An error occurred while uploading the firmware")
        {
        }
    }
}