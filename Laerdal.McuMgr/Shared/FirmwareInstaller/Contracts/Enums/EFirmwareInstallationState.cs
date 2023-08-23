namespace Laerdal.McuMgr.FirmwareInstaller.Contracts.Enums
{
    public enum EFirmwareInstallationState //these must mirror the java enum values of EFirmwareInstallationState
    {
        None = 0,
        Idle = 1,
        Validating = 2,
        Uploading = 3,
        Paused = 4,
        Testing = 5,
        Confirming = 6,
        Resetting = 7,
        Complete = 8,
        Cancelled = 9,
        Error = 10
    }
}