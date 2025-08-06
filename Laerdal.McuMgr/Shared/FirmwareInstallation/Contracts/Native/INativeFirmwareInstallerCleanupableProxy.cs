namespace Laerdal.McuMgr.FirmwareInstallation.Contracts.Native
{
    internal interface INativeFirmwareInstallerCleanupableProxy
    {
        void CleanupResourcesOfLastInstallation();
    }
}