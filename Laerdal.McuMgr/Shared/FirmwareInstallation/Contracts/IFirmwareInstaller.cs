// ReSharper disable UnusedMember.Global
// ReSharper disable EventNeverSubscribedTo.Global

using System;

namespace Laerdal.McuMgr.FirmwareInstallation.Contracts
{
    /// <summary>Upgrades the firmware on a specific Nordic-chip-based BLE device</summary>
    public interface IFirmwareInstaller :
        IFirmwareInstallerQueryable,
        IFirmwareInstallerCommandable,
        IFirmwareInstallerEventSubscribable,
        IDisposable
    {
    }
}
