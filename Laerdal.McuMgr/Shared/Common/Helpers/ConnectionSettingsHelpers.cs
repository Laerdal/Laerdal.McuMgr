using Laerdal.McuMgr.Common.Constants;

namespace Laerdal.McuMgr.Common.Helpers
{
    static internal class ConnectionSettingsHelpers
    {
        static public (int? byteAlignment, int? pipelineDepth, int? initialMtuSize, int? windowCapacity, int? memoryAlignment)? GetFailsafeConnectionSettingsIfConnectionProvedToBeUnstable(
            int triesCount,
            int maxTriesCount,
            int suspiciousTransportFailuresCount
        )
        {
            var isConnectionTooUnstableForUploading = triesCount >= 2 && (triesCount == maxTriesCount || triesCount >= 3 && suspiciousTransportFailuresCount >= 2);
            if (!isConnectionTooUnstableForUploading)
                return null;

            var byteAlignment = AppleTidbits.BleConnectionSettings.FailSafes.ByteAlignment; //        ios + maccatalyst
            var pipelineDepth = AppleTidbits.BleConnectionSettings.FailSafes.PipelineDepth; //        ios + maccatalyst
            var initialMtuSize = AndroidTidbits.BleConnectionSettings.FailSafes.InitialMtuSize; //    android    when noticing persistent failures when uploading we resort
            var windowCapacity = AndroidTidbits.BleConnectionSettings.FailSafes.WindowCapacity; //    android    to forcing the most failsafe settings we know of just in case
            var memoryAlignment = AndroidTidbits.BleConnectionSettings.FailSafes.MemoryAlignment; //  android    we manage to salvage this situation (works with SamsungA8 android tablets)

            return (byteAlignment: byteAlignment, pipelineDepth: pipelineDepth, initialMtuSize: initialMtuSize, windowCapacity: windowCapacity, memoryAlignment: memoryAlignment);
        }
        
        static public (int? byteAlignment, int? pipelineDepth, int? initialMtuSize, int? windowCapacity, int? memoryAlignment)? GetFailSafeConnectionSettingsIfHostDeviceIsProblematic(
            string hostDeviceManufacturer,
            string hostDeviceModel,
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
            if (isUsingDefaultAppleSettings && AppleTidbits.KnownProblematicDevices.Contains((hostDeviceManufacturer, hostDeviceModel)))
            {
                return (
                    byteAlignment: AppleTidbits.BleConnectionSettings.FailSafes.ByteAlignment,
                    pipelineDepth: AppleTidbits.BleConnectionSettings.FailSafes.PipelineDepth,
                    initialMtuSize: null, //only applies to android
                    windowCapacity: null, //only applies to android
                    memoryAlignment: null //only applies to android
                );
            }

            var isUsingDefaultAndroidSettings = initialMtuSize == null && windowCapacity == null && memoryAlignment == null;
            if (isUsingDefaultAndroidSettings && AndroidTidbits.KnownProblematicDevices.Contains((hostDeviceManufacturer, hostDeviceModel)))
            {
                return (
                    byteAlignment: null, //only applies to apple
                    pipelineDepth: null, //only applies to apple
                    initialMtuSize: AndroidTidbits.BleConnectionSettings.FailSafes.InitialMtuSize,
                    windowCapacity: AndroidTidbits.BleConnectionSettings.FailSafes.WindowCapacity,
                    memoryAlignment: AndroidTidbits.BleConnectionSettings.FailSafes.MemoryAlignment
                );
            }

            return null;
        }
    }
}