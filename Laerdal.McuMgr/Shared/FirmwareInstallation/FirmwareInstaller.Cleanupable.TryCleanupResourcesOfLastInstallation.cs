namespace Laerdal.McuMgr.FirmwareInstallation
{
    public partial class FirmwareInstaller
    {
        public void TryCleanupResourcesOfLastInstallation() => NativeFirmwareInstallerProxy?.TryCleanupResourcesOfLastInstallation();
    }
}
