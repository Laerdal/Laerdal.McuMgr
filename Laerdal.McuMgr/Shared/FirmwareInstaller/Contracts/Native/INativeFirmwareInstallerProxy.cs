using System;

namespace Laerdal.McuMgr.FirmwareInstaller.Contracts.Native
{
    internal interface INativeFirmwareInstallerProxy :
        INativeFirmwareInstallerCommandableProxy,
        INativeFirmwareInstallerQueryableProxy,
        INativeFirmwareInstallerCleanupableProxy,
        INativeFirmwareInstallerCallbacksProxy,
        IDisposable
    {
        string Nickname { get; set; }
    }
}