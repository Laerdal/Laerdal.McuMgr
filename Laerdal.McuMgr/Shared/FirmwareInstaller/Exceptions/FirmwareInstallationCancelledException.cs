using System;

namespace Laerdal.McuMgr.FirmwareInstaller.Exceptions
{
    public class FirmwareInstallationCancelledException : Exception, IFirmwareInstallationRelatedException
    {
        public FirmwareInstallationCancelledException() : base("Firmware installation was cancelled")
        {
        }
    }
}