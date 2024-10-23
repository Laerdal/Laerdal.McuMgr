using System.Collections.Generic;
using System.Linq;

namespace Laerdal.McuMgr.Common.Constants
{
    public readonly struct AndroidTidbits
    {
        /// <summary>List of known problematic android-devices that have issues with BLE connection stability and have to use fail-safe ble settings to work.</summary><br/><br/>
        /// Inspired by <a href="https://github.com/polarofficial/polar-ble-sdk/blob/master/sources/Android/android-communications/library/src/main/java/com/polar/androidcommunications/common/ble/PhoneUtils.kt"/>
        static public HashSet<(string Manufacturer, string DeviceModel)> KnownProblematicDevices { get; } = new (string Manufacturer, string DeviceModel)[]
            {
                ("Motorola", "moto g20"),
                ("Motorola", "moto e20"),
                ("Motorola", "moto e30"),
                ("Motorola", "moto e32"),
                ("Motorola", "moto e40"),

                ("Nokia", "Nokia G21"),
                ("Nokia", "Nokia G11"),
                ("Nokia", "Nokia T20"),

                ("Realme", "RMX3261"), //C21Y
                ("Realme", "RMX3262"), //C21Y
                ("Realme", "RMX3265"), //C25Y
                ("Realme", "RMX3269"), //C25Y
                ("Realme", "RMP2105"), //Pad Mini
                ("Realme", "RMP2106"), //Pad Mini

                ("Infinix", "Infinix X675"), //Hot 11 2022

                ("HTC", "Wildfire E2 plus"),

                ("Micromax", "IN_2b"),
                ("Micromax", "IN_2c"),

                ("Samsung", "SM-X200"), //Galaxy Tab A8
            }
            .Select(x => (x.Manufacturer.Trim().ToLowerInvariant(), x.DeviceModel.Trim().ToLowerInvariant())) //vital
            .ToHashSet();
        
        /// <summary>
        /// Failsafe settings for the BLE connection used in Androids to perform various operations: installing firmware, resetting the device, erasing firmwares, uploading files,
        /// downloading files. These settings are enforced automagically when the ble connection turns out to be unstable and unreliable during the aforementioned operations.
        /// The settings are editable can be adjusted to fit future needs. 
        /// </summary>
        public readonly struct BleConnectionSettings
        {
            public readonly struct FailSafes
            {
                static public int InitialMtuSize { get; set; } = 23;
                static public int WindowCapacity { get; set; } = 1;
                static public int MemoryAlignment { get; set; } = 1;    
            }
        }
    }
}
