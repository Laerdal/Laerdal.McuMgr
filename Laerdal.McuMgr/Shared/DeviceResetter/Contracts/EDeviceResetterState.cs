namespace Laerdal.McuMgr.DeviceResetter.Contracts
{
    public enum EDeviceResetterState
    {
        None = 0,
        Idle = 1,
        Resetting = 2,
        Complete = 3,
        Failed = 4
    }
}