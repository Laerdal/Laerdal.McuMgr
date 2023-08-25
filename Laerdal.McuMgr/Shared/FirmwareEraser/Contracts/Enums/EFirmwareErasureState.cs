namespace Laerdal.McuMgr.FirmwareEraser.Contracts.Enums
{
    public enum EFirmwareErasureState
    {
        None = 0,
        Idle = 1,
        Erasing = 2,
        Complete = 3,
        Failed = 4,
    }
}