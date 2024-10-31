namespace Laerdal.McuMgr.DeviceResetter.Contracts.Enums
{
    public enum EDeviceResetterInitializationVerdict
    {
        Success = 0,
        FailedErrorUponCommencing = 1,
        FailedOtherResetAlreadyInProgress = 2,
    }
}