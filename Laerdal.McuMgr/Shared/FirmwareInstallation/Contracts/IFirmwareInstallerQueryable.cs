namespace Laerdal.McuMgr.FirmwareInstallation.Contracts
{
    public interface IFirmwareInstallerQueryable
    {
        /// <summary>Holds the last error message emitted</summary>
        string LastFatalErrorMessage { get; }
    }
}