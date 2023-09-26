namespace Laerdal.McuMgr.FirmwareInstaller.Contracts.Native
{
    internal interface INativeFirmwareInstallerProxy : INativeFirmwareInstallerCommandableProxy, INativeFirmwareInstallerQueryableProxy, INativeFirmwareInstallerCallbacksProxy
    {
        string Nickname { get; set; }
    }
}