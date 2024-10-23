using System;
using System.Collections.Generic;
using System.Linq;

namespace Laerdal.McuMgr.Common.Constants
{
    public readonly struct AppleTidbits
    {
        // ReSharper disable once CollectionNeverUpdated.Global
        /// <summary>List of known problematic android-devices that have issues with BLE connection stability and have to use fail-safe ble settings to work.</summary><br/><br/>
        /// Inspired by <a href="https://github.com/polarofficial/polar-ble-sdk/blob/master/sources/Android/android-communications/library/src/main/java/com/polar/androidcommunications/common/ble/PhoneUtils.kt"/>
        static public HashSet<(string Manufacturer, string DeviceModel)> KnownProblematicDevices { get; } = new (string Manufacturer, string DeviceModel)[]
            {
                // ("Apple", "XYZ"), // just a placeholder - no apple devices are known to have issues with BLE connection stability at the time of this writing
            }
            .Select(x => (x.Manufacturer.Trim().ToLowerInvariant(), x.DeviceModel.Trim().ToLowerInvariant()))
            .ToHashSet();
        
        /// <summary>
        /// Failsafe settings for the BLE connection used in Apple platforms (iOS / MacCatalyst) to perform various operations: installing firmware, resetting the device,
        /// erasing firmwares, uploading files, downloading files. These settings are enforced automagically when the ble connection turns out to be unstable and unreliable
        /// during the aforementioned operations. The settings are editable can be adjusted to fit future needs. 
        /// </summary>
        public readonly struct BleConnectionSettings
        {
            public readonly struct FailSafes
            {
                static public int PipelineDepth { get; set; } = 1;
                static public int ByteAlignment { get; set; } = 1;    
            }
        }
    }
}