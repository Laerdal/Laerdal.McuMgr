namespace Laerdal.McuMgr.FirmwareEraser.Contracts
{
    public enum EFirmwareErasureState
    {
        None = 0,
        Idle = 1,
        Erasing = 2,
        Complete = 3,
        Error = 4,
    }
}