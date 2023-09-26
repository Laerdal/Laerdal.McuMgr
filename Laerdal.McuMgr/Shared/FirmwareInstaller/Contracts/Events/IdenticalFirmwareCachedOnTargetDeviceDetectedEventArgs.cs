using Laerdal.McuMgr.Common.Events;
using Laerdal.McuMgr.FirmwareInstaller.Contracts.Enums;

namespace Laerdal.McuMgr.FirmwareInstaller.Contracts.Events
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