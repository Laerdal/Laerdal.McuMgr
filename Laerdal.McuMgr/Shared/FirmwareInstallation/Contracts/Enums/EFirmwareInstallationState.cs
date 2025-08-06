namespace Laerdal.McuMgr.FirmwareInstallation.Contracts.Enums
{
    public enum EFirmwareInstallationState //these must mirror the enum values of E[Android|IOS]FirmwareInstallationState
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
        Cancelling = 9,
        Cancelled = 10,
        Error = 11
    }
}