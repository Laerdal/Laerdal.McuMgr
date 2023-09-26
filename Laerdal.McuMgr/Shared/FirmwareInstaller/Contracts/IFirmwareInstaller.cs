// ReSharper disable UnusedMember.Global
// ReSharper disable EventNeverSubscribedTo.Global

namespace Laerdal.McuMgr.FirmwareInstaller.Contracts
{
    /// <summary>Upgrades the firmware on a specific Nordic-chip-based BLE device</summary>
    public interface IFirmwareInstaller : IFirmwareInstallerQueryable, IFirmwareInstallerEventSubscribable, IFirmwareInstallerCommandable
    {
    }
}
