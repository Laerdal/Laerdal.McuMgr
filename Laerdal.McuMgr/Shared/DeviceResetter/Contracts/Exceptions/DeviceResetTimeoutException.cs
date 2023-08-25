using System;

namespace Laerdal.McuMgr.DeviceResetter.Contracts.Exceptions
{
    public sealed class DeviceResetTimeoutException : DeviceResetterErroredOutException, IDeviceResetterException
    {
        public DeviceResetTimeoutException(int timeoutInMs, Exception innerException)
            : base($"Failed to reset/reboot the device within {timeoutInMs}ms", innerException)
        {
        }
    }
}