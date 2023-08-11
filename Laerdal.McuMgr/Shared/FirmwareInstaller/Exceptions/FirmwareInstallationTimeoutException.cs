using System;

namespace Laerdal.McuMgr.FirmwareInstaller.Exceptions
{
    public sealed class FirmwareInstallationTimeoutException : FirmwareInstallationErroredOutException
    {
        public FirmwareInstallationTimeoutException(int timeoutInMs, Exception innerException)
            : base($"Failed to reset/reboot the device within {timeoutInMs}ms", innerException)
        {
        }
    }
}