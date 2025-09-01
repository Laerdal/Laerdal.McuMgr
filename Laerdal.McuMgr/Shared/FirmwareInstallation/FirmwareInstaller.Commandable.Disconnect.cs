namespace Laerdal.McuMgr.FirmwareInstallation
{
    public partial class FirmwareInstaller
    {
        public void Disconnect() => _nativeFirmwareInstallerProxy?.Disconnect();
    }
}
