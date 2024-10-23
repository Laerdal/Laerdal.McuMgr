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

            if (AppleTidbits.KnownProblematicDevices.Contains((hostDeviceManufacturer, hostDeviceModel))
                && (pipelineDepth ?? 1) == 1
                && (byteAlignment ?? 1) == 1)
            {
                return (
                    byteAlignment: AppleTidbits.FailSafeBleConnectionSettings.ByteAlignment,
                    pipelineDepth: AppleTidbits.FailSafeBleConnectionSettings.PipelineDepth,
                    initialMtuSize: AndroidTidbits.FailSafeBleConnectionSettings.InitialMtuSize,
                    windowCapacity: AndroidTidbits.FailSafeBleConnectionSettings.WindowCapacity,
                    memoryAlignment: AndroidTidbits.FailSafeBleConnectionSettings.MemoryAlignment
                );
            }

            if (AndroidTidbits.KnownProblematicDevices.Contains((hostDeviceManufacturer, hostDeviceModel))
                && initialMtuSize == null
                && (windowCapacity ?? 1) == 1
                && (memoryAlignment ?? 1) == 1)
            {
                return (
                    byteAlignment: AppleTidbits.FailSafeBleConnectionSettings.ByteAlignment,
                    pipelineDepth: AppleTidbits.FailSafeBleConnectionSettings.PipelineDepth,
                    initialMtuSize: AndroidTidbits.FailSafeBleConnectionSettings.InitialMtuSize,
                    windowCapacity: AndroidTidbits.FailSafeBleConnectionSettings.WindowCapacity,
                    memoryAlignment: AndroidTidbits.FailSafeBleConnectionSettings.MemoryAlignment
                );
            }

            return (
                byteAlignment: byteAlignment, pipelineDepth: pipelineDepth,
                initialMtuSize: initialMtuSize, windowCapacity: windowCapacity, memoryAlignment: memoryAlignment
            );
        }
    }
}