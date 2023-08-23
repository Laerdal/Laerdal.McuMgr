// ReSharper disable RedundantExtendsListEntry

using System;

namespace Laerdal.McuMgr.FirmwareInstaller.Contracts.Exceptions
{
    public sealed class FirmwareInstallationTimeoutException : FirmwareInstallationErroredOutException, IFirmwareInstallationException
    {
        public FirmwareInstallationTimeoutException(int timeoutInMs, Exception innerException)
            : base($"Failed to reset/reboot the device within {timeoutInMs}ms", innerException)
        {
        }
    }
}