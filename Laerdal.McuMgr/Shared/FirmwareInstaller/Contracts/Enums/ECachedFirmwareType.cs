namespace Laerdal.McuMgr.FirmwareInstaller.Contracts.Enums
{
    public enum ECachedFirmwareType
    {
        /// <summary>Means that the cached firmware on the device is currently the active firmware that the device is using so we don't even need to activate it</summary>
        CachedAndActive = 0,
        
        /// <summary>Means that the cached firmware on the device is currently inactive so we simply need to activate it</summary>
        CachedButInactive = 1,
    }
}