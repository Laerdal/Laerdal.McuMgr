using Laerdal.McuMgr.Common.Constants;

namespace Laerdal.McuMgr.Common.Helpers
{
    static internal class ConnectionSettingsHelpers
    {
        static public (int? byteAlignment, int? pipelineDepth, int? initialMtuSize, int? windowCapacity, int? memoryAlignment) GetFailSafeConnectionSettingsIfHostDeviceIsProblematic(
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
                    initialMtuSize: AndroidTidbits.BleConnectionSettings.FailSafes.InitialMtuSize,
                    windowCapacity: AndroidTidbits.BleConnectionSettings.FailSafes.WindowCapacity,
                    memoryAlignment: AndroidTidbits.BleConnectionSettings.FailSafes.MemoryAlignment
                );
            }

            var isUsingDefaultAndroidSettings = initialMtuSize == null && windowCapacity == null && memoryAlignment == null;
            if (isUsingDefaultAndroidSettings && AndroidTidbits.KnownProblematicDevices.Contains((hostDeviceManufacturer, hostDeviceModel)))
            {
                return (
                    byteAlignment: AppleTidbits.BleConnectionSettings.FailSafes.ByteAlignment,
                    pipelineDepth: AppleTidbits.BleConnectionSettings.FailSafes.PipelineDepth,
                    initialMtuSize: AndroidTidbits.BleConnectionSettings.FailSafes.InitialMtuSize,
                    windowCapacity: AndroidTidbits.BleConnectionSettings.FailSafes.WindowCapacity,
                    memoryAlignment: AndroidTidbits.BleConnectionSettings.FailSafes.MemoryAlignment
                );
            }

            return (
                byteAlignment: byteAlignment, pipelineDepth: pipelineDepth,
                initialMtuSize: initialMtuSize, windowCapacity: windowCapacity, memoryAlignment: memoryAlignment
            );
        }
    }
}