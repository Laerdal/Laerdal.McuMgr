using Laerdal.McuMgr.Common.Events;
using Laerdal.McuMgr.FirmwareInstallation.Contracts.Enums;

namespace Laerdal.McuMgr.FirmwareInstallation.Contracts.Events
{
    public readonly struct IdenticalFirmwareCachedOnTargetDeviceDetectedEventArgs : IMcuMgrEventArgs
    {
        public ECachedFirmwareType CachedFirmwareType { get; }

        public IdenticalFirmwareCachedOnTargetDeviceDetectedEventArgs(ECachedFirmwareType type)
        {
            CachedFirmwareType = type;
        }
    }
}