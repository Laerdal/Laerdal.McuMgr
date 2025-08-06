namespace Laerdal.McuMgr.FirmwareInstallation.Contracts.Native
{
    internal interface INativeFirmwareInstallerQueryableProxy
    {
        string LastFatalErrorMessage { get; }
    }
}