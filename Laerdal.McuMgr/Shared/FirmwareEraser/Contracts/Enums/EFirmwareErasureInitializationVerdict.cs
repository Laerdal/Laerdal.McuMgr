namespace Laerdal.McuMgr.FirmwareEraser.Contracts.Enums
{
    public enum EFirmwareErasureInitializationVerdict
    {
        Success = 0,
        FailedErrorUponCommencing = 1,
        FailedOtherErasureAlreadyInProgress = 2,
    }
}