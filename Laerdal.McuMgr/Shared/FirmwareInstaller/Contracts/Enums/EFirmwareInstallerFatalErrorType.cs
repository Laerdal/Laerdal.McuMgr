namespace Laerdal.McuMgr.FirmwareInstaller.Contracts.Enums
{
    public enum EFirmwareInstallerFatalErrorType //these must mirror the enum values of E[Android|IOS]FirmwareInstallerFatalErrorType
    {
        Generic = 0,
        InvalidSettings = 1,
        InvalidDataFile = 2,
        DeploymentFailed = 3,
        FirmwareImageSwapTimeout = 4
    }
}