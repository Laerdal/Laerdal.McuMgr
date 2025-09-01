namespace Laerdal.McuMgr.FirmwareInstallation.Contracts.Native
{
    public interface INativeFirmwareInstallerQueryableProxy
    {
        string LastFatalErrorMessage { get; }
    }
}