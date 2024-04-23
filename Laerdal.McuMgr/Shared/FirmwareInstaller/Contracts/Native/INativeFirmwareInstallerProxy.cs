namespace Laerdal.McuMgr.FirmwareInstaller.Contracts.Native
{
    internal interface INativeFirmwareInstallerProxy :
        INativeFirmwareInstallerCommandableProxy,
        INativeFirmwareInstallerQueryableProxy,
        INativeFirmwareInstallerCleanupableProxy,
        INativeFirmwareInstallerCallbacksProxy
    {
        string Nickname { get; set; }
    }
}