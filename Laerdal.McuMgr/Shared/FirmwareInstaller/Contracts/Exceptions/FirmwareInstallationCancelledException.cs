using System;

namespace Laerdal.McuMgr.FirmwareInstaller.Contracts.Exceptions
{
    public class FirmwareInstallationCancelledException : OperationCanceledException, IFirmwareInstallationException
    {
        public FirmwareInstallationCancelledException() : base("Firmware installation was cancelled")
        {
        }
    }
}