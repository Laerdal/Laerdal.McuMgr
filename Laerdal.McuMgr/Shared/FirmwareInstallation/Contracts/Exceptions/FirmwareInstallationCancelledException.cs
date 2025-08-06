using System;

namespace Laerdal.McuMgr.FirmwareInstallation.Contracts.Exceptions
{
    public class FirmwareInstallationCancelledException : OperationCanceledException, IFirmwareInstallationException
    {
        public FirmwareInstallationCancelledException() : base("Firmware installation was cancelled")
        {
        }
    }
}