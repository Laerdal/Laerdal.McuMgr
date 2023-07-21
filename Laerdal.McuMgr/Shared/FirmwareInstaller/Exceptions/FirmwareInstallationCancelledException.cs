using System;

namespace Laerdal.McuMgr.FirmwareInstaller.Exceptions
{
    public class FirmwareInstallationCancelledException : Exception
    {
        public FirmwareInstallationCancelledException() : base("Firmware installation was cancelled")
        {
        }
    }
}