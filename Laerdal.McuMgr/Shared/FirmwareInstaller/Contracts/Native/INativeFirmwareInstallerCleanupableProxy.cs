namespace Laerdal.McuMgr.FirmwareInstaller.Contracts.Native
{
    internal interface INativeFirmwareInstallerCleanupableProxy
    {
        void CleanupResourcesOfLastInstallation();
    }
}