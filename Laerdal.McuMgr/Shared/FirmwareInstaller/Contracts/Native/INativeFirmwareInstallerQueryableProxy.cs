namespace Laerdal.McuMgr.FirmwareInstaller.Contracts.Native
{
    internal interface INativeFirmwareInstallerQueryableProxy
    {
        string LastFatalErrorMessage { get; }
    }
}