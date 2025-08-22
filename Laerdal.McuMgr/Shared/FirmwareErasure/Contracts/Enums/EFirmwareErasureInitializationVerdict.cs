namespace Laerdal.McuMgr.FirmwareErasure.Contracts.Enums
{
    public enum EFirmwareErasureInitializationVerdict
    {
        Success = 0,
        FailedErrorUponCommencing = 1,
        FailedOtherErasureAlreadyInProgress = 2,
    }
}