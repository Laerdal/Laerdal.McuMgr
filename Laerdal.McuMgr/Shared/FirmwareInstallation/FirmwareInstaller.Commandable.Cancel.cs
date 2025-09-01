namespace Laerdal.McuMgr.FirmwareInstallation
{
    public partial class FirmwareInstaller
    {
        public void Cancel() => _nativeFirmwareInstallerProxy?.Cancel();
    }
}
