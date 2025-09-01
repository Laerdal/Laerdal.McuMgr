namespace Laerdal.McuMgr.FirmwareInstallation
{
    public partial class FirmwareInstaller
    {
        public void TryCleanupResourcesOfLastInstallation() => _nativeFirmwareInstallerProxy?.TryCleanupResourcesOfLastInstallation();
    }
}
