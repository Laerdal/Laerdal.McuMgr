using Laerdal.McuMgr.Common.Constants;

namespace Laerdal.McuMgr.Common.Helpers
{
    static internal class ConnectionSettingsHelpers
    {
        static public (int? byteAlignment, int? pipelineDepth, int? initialMtuSize, int? windowCapacity, int? memoryAlignment) GetFailSafeConnectionSettingsIfHostDeviceIsProblematic_(
            string hostDeviceManufacturer_,
            string hostDeviceModel_,
            int? pipelineDepth_ = null,
            int? byteAlignment_ = null,
            int? initialMtuSize_ = null,
            int? windowCapacity_ = null,
            int? memoryAlignment_ = null
        )
        {
            hostDeviceModel_ = (hostDeviceModel_ ?? "").Trim().ToLowerInvariant();
            hostDeviceManufacturer_ = (hostDeviceManufacturer_ ?? "").Trim().ToLowerInvariant();

            if (AppleTidbits.KnownProblematicDevices.Contains((hostDeviceManufacturer_, hostDeviceModel_))
                && (pipelineDepth_ ?? 1) == 1
                && (byteAlignment_ ?? 1) == 1)
            {
                return (
                    byteAlignment: AppleTidbits.FailSafeBleConnectionSettings.ByteAlignment,
                    pipelineDepth: AppleTidbits.FailSafeBleConnectionSettings.PipelineDepth,
                    initialMtuSize: AndroidTidbits.FailSafeBleConnectionSettings.InitialMtuSize,
                    windowCapacity: AndroidTidbits.FailSafeBleConnectionSettings.WindowCapacity,
                    memoryAlignment: AndroidTidbits.FailSafeBleConnectionSettings.MemoryAlignment
                );
            }

            if (AndroidTidbits.KnownProblematicDevices.Contains((hostDeviceManufacturer_, hostDeviceModel_))
                && initialMtuSize_ == null
                && (windowCapacity_ ?? 1) == 1
                && (memoryAlignment_ ?? 1) == 1)
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
                byteAlignment: byteAlignment_, pipelineDepth: pipelineDepth_,
                initialMtuSize: initialMtuSize_, windowCapacity: windowCapacity_, memoryAlignment: memoryAlignment_
            );
        }
    }
}