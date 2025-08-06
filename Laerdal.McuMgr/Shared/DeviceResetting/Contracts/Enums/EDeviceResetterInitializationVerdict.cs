namespace Laerdal.McuMgr.DeviceResetting.Contracts.Enums
{
    public enum EDeviceResetterInitializationVerdict
    {
        Success = 0,
        FailedErrorUponCommencing = 1,
        FailedOtherResetAlreadyInProgress = 2,
    }
}