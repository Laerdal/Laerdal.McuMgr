namespace Laerdal.McuMgr.FirmwareInstaller.Contracts.Native
{
    internal interface INativeFirmwareInstallerProxy : INativeFirmwareInstallerCommandsProxy, INativeFirmwareInstallerQueryableProxy, INativeFirmwareInstallerCallbacksProxy
    {
        string Nickname { get; set; }
    }
}