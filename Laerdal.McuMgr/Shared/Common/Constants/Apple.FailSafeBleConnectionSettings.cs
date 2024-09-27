namespace Laerdal.McuMgr.Common.Constants
{
    public readonly struct AppleTidbits
    {
        /// <summary>
        /// Failsafe settings for the BLE connection used in Apple platforms (iOS / MacCatalyst) to perform various operations: installing firmware, resetting the device,
        /// erasing firmwares, uploading files, downloading files. These settings are enforced automagically when the ble connection turns out to be unstable and unreliable
        /// during the aforementioned operations. The settings are editable can be adjusted to fit future needs. 
        /// </summary>
        public readonly struct FailSafeBleConnectionSettings
        {
            static public int PipelineDepth { get; set; } = 1;
            static public int ByteAlignment { get; set; } = 1;
        }
    }
}