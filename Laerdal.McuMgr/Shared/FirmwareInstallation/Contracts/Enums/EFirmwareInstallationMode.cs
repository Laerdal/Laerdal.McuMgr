namespace Laerdal.McuMgr.FirmwareInstallation.Contracts.Enums
{
    public enum EFirmwareInstallationMode //this must mirror the java enum values of E[Android|iOS]FirmwareInstallationMode 
    {
        TestOnly = 0,
        ConfirmOnly = 1,
        TestAndConfirm = 2
    }
}