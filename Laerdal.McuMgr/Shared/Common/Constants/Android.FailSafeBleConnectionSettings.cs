using System.Collections.Generic;
using System.Linq;

namespace Laerdal.McuMgr.Common.Constants
{
    public readonly struct AndroidTidbits
    {
        /// <summary>List of known problematic android-devices that have issues with BLE connection stability and have to use fail-safe ble settings to work.</summary><br/><br/>
        /// Inspired by <a href="https://github.com/polarofficial/polar-ble-sdk/blob/master/sources/Android/android-communications/library/src/main/java/com/polar/androidcommunications/common/ble/PhoneUtils.kt"/>
        static public HashSet<(string DeviceModel, string Manufacturer)> KnownProblematicDevices { get; } = new (string DeviceModel, string Manufacturer)[]
            {
                ("moto g20", "Motorola"),
                ("moto e20", "Motorola"),
                ("moto e30", "Motorola"),
                ("moto e32", "Motorola"),
                ("moto e40", "Motorola"),

                ("Nokia G21", "Nokia"),
                ("Nokia G11", "Nokia"),
                ("Nokia T20", "Nokia"),

                ("RMX3261", "Realme"), //C21Y
                ("RMX3262", "Realme"), //C21Y
                ("RMX3265", "Realme"), //C25Y
                ("RMX3269", "Realme"), //C25Y
                ("RMP2105", "Realme"), //Pad Mini
                ("RMP2106", "Realme"), //Pad Mini

                ("Infinix X675", "Infinix"), //Hot 11 2022

                ("Wildfire E2 plus", "HTC"),

                ("IN_2b", "Micromax"),
                ("IN_2c", "Micromax"),

                ("SM-X200", "Samsung"), //Galaxy Tab A8
            }
            .Select(x => (DeviceModel: x.DeviceModel.Trim().ToLowerInvariant(), Manufacturer: x.Manufacturer.Trim().ToLowerInvariant())) //vital
            .ToHashSet();
        
        /// <summary>
        /// Failsafe settings for the BLE connection used in Androids to perform various operations: installing firmware, resetting the device, erasing firmwares, uploading files,
        /// downloading files. These settings are enforced automagically when the ble connection turns out to be unstable and unreliable during the aforementioned operations.
        /// The settings are editable can be adjusted to fit future needs. 
        /// </summary>
        public readonly struct BleConnectionFailsafeSettings
        {
            public readonly struct ForUploading
            {
                static public int InitialMtuSize { get; set; } = 23;
                static public int WindowCapacity { get; set; } = 1;
                static public int MemoryAlignment { get; set; } = 1;    
            }
            
            public readonly struct ForDownloading
            {
                static public int InitialMtuSize { get; set; } = 50; //00
                //static public int WindowCapacity { get; set; }= 1; //10
                
                //00  oddly enough when it comes to downloading using a value of 23 is not supported even by healthy devices   so we have to use a greater value   it is worth noting
                //    however that even among healthy devices the lowest value supported varies   some can go as low as 25 while others only as low as 30   go figure
                //
                //10  window capacity could be supported in the future   currently its not support though   https://github.com/NordicSemiconductor/Android-nRF-Connect-Device-Manager/issues/188#issuecomment-2391146897
            }
        }
    }
}
