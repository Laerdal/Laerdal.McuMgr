using System;
using Laerdal.McuMgr.Common.Constants;

namespace Laerdal.McuMgr.Common.Helpers
{
    static internal class ConnectionSettingsHelpers
    {
        static public (int? byteAlignment, int? pipelineDepth, int? initialMtuSize, int? windowCapacity, int? memoryAlignment)? GetFailsafeConnectionSettingsIfConnectionProvedToBeUnstable(
            bool uploadingNotDownloading,
            int triesCount,
            int maxTriesCount,
            int suspiciousTransportFailuresCount,
            int? resortToFailSafeSettingOnSuspiciousFailureCount = null //todo   we should consider making configurable via the options object!
        )
        {
            resortToFailSafeSettingOnSuspiciousFailureCount ??= Math.Min(10, maxTriesCount - 3); // last few tries reserved for the last-ditch effort to salvage the connection!
            
            var isConnectionTooUnstableForUploading = triesCount >= 2 && (triesCount == maxTriesCount || triesCount >= 3 && suspiciousTransportFailuresCount >= resortToFailSafeSettingOnSuspiciousFailureCount);
            if (!isConnectionTooUnstableForUploading)
                return null;
            
            var byteAlignment = uploadingNotDownloading // ios + maccatalyst
                ? AppleTidbits.BleConnectionFailsafeSettings.ForUploading.ByteAlignment
                : (int?)null; //byteAlignment is not applicable for downloads
            var pipelineDepth = uploadingNotDownloading // ios + maccatalyst
                ? AppleTidbits.BleConnectionFailsafeSettings.ForUploading.PipelineDepth
                : (int?)null; //pipelineDepth is not applicable for downloads
            
            var initialMtuSize = uploadingNotDownloading  //android                                when noticing persistent failures when uploading/downloading we
                ? AndroidTidbits.BleConnectionFailsafeSettings.ForUploading.InitialMtuSize //      resort to forcing the most failsafe settings we know of just in case
                : AndroidTidbits.BleConnectionFailsafeSettings.ForDownloading.InitialMtuSize; //   we manage to salvage this situation (works with SamsungA8 android tablets)
            var windowCapacity = uploadingNotDownloading
                ? AndroidTidbits.BleConnectionFailsafeSettings.ForUploading.WindowCapacity
                : (int?)null; //window-capacity is not applicable for downloads    
            var memoryAlignment = uploadingNotDownloading
                ? AndroidTidbits.BleConnectionFailsafeSettings.ForUploading.MemoryAlignment
                : (int?)null; //memory-alignment is not applicable for downloads

            return (byteAlignment: byteAlignment, pipelineDepth: pipelineDepth, initialMtuSize: initialMtuSize, windowCapacity: windowCapacity, memoryAlignment: memoryAlignment);
        }
        
        static public (int? byteAlignment, int? pipelineDepth, int? initialMtuSize, int? windowCapacity, int? memoryAlignment)? GetFailSafeConnectionSettingsIfHostDeviceIsProblematic(
            bool uploadingNotDownloading,
            string hostDeviceModel,
            string hostDeviceManufacturer,
            int? pipelineDepth = null,
            int? byteAlignment = null,
            int? initialMtuSize = null,
            int? windowCapacity = null,
            int? memoryAlignment = null
        )
        {
            hostDeviceModel = (hostDeviceModel ?? "").Trim().ToLowerInvariant();
            hostDeviceManufacturer = (hostDeviceManufacturer ?? "").Trim().ToLowerInvariant();

            var isUsingDefaultAppleSettings = pipelineDepth == null && byteAlignment == null; 
            if (isUsingDefaultAppleSettings && AppleTidbits.KnownProblematicDevices.Contains((DeviceModel: hostDeviceModel, Manufacturer: hostDeviceManufacturer)))
            {
                return uploadingNotDownloading
                    ? ( //uploading
                        byteAlignment: AppleTidbits.BleConnectionFailsafeSettings.ForUploading.ByteAlignment,
                        pipelineDepth: AppleTidbits.BleConnectionFailsafeSettings.ForUploading.PipelineDepth,
                        initialMtuSize: null, //only applies to android
                        windowCapacity: null, //only applies to android
                        memoryAlignment: null //only applies to android
                    )
                    : ( //downloading
                        byteAlignment: null, //placeholder value   currently there are no known apple devices that have issues with BLE connection stability
                        pipelineDepth: null, //placeholder value   currently there are no known apple devices that have issues with BLE connection stability
                        initialMtuSize: null, //only applies to android
                        windowCapacity: null, //only applies to android
                        memoryAlignment: null //only applies to android
                    );
            }

            var isUsingDefaultAndroidSettings = initialMtuSize == null && windowCapacity == null && memoryAlignment == null;
            if (isUsingDefaultAndroidSettings && AndroidTidbits.KnownProblematicDevices.Contains((DeviceModel: hostDeviceModel, Manufacturer: hostDeviceManufacturer)))
            {
                return uploadingNotDownloading
                    ? ( //uploading
                        byteAlignment: null, //only applies to apple
                        pipelineDepth: null, //only applies to apple
                        initialMtuSize: AndroidTidbits.BleConnectionFailsafeSettings.ForUploading.InitialMtuSize,
                        windowCapacity: AndroidTidbits.BleConnectionFailsafeSettings.ForUploading.WindowCapacity,
                        memoryAlignment: AndroidTidbits.BleConnectionFailsafeSettings.ForUploading.MemoryAlignment
                    )
                    : ( //downloading
                        byteAlignment: null, //only applies to apple
                        pipelineDepth: null, //only applies to apple
                        initialMtuSize: AndroidTidbits.BleConnectionFailsafeSettings.ForDownloading.InitialMtuSize,
                        windowCapacity: null, // currently it doesnt apply to android downloads   but nordic might consider adding it in the future
                        memoryAlignment: null // doesnt apply to android downloads
                    );
            }

            return null;
        }
    }
}