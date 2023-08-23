using System;

namespace Laerdal.McuMgr.FirmwareInstaller.Exceptions
{
    public class FirmwareInstallationCancelledException : Exception, IFirmwareInstallationException
    {
        public FirmwareInstallationCancelledException() : base("Firmware installation was cancelled")
        {
        }
    }
}