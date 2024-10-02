namespace Laerdal.McuMgr.Common.Constants
{
    public readonly struct AndroidTidbits
    {
        /// <summary>
        /// Failsafe settings for the BLE connection used in Androids to perform various operations: installing firmware, resetting the device, erasing firmwares, uploading files,
        /// downloading files. These settings are enforced automagically when the ble connection turns out to be unstable and unreliable during the aforementioned operations.
        /// The settings are editable can be adjusted to fit future needs. 
        /// </summary>
        public readonly struct FailSafeBleConnectionSettings
        {
            static public int InitialMtuSize { get; set; } = 23; // applies to android only
            static public int WindowCapacity { get; set; } = 1; //  applies to android only
            static public int MemoryAlignment { get; set; } = 1; // applies to android only    
        }
    }
}
