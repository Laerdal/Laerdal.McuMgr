using System;

namespace Laerdal.McuMgr.FirmwareInstallation.Contracts.Native
{
    public interface INativeFirmwareInstallerProxy :
        IDisposable,
        INativeFirmwareInstallerQueryableProxy,
        INativeFirmwareInstallerCallbacksProxy,
        INativeFirmwareInstallerCommandableProxy,
        INativeFirmwareInstallerCleanupableProxy
    {
        string Nickname { get; set; }
    }
}