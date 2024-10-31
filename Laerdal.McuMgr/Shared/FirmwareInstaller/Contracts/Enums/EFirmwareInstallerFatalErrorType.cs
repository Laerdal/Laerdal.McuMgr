namespace Laerdal.McuMgr.FirmwareInstaller.Contracts.Enums
{
    public enum EFirmwareInstallerFatalErrorType //these must mirror the enum values of E[Android|IOS]FirmwareInstallerFatalErrorType
    {
        Generic = 0,
        InvalidSettings = 1,
        InvalidFirmware = 2,
        DeploymentFailed = 3,
        FirmwareImageSwapTimeout = 4,
        FirmwareUploadingErroredOut = 5,
        FailedInstallationAlreadyInProgress = 6,
    }
}
