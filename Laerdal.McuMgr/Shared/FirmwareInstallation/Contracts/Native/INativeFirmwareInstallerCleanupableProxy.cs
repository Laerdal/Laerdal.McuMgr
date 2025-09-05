namespace Laerdal.McuMgr.FirmwareInstallation.Contracts.Native
{
    public interface INativeFirmwareInstallerCleanupableProxy
    {
        void TryCleanupResourcesOfLastInstallation();
    }
}