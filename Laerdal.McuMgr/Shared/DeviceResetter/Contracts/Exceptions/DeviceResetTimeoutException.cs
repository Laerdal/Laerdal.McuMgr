namespace Laerdal.McuMgr.DeviceResetter.Contracts.Exceptions
{
    public sealed class DeviceResetTimeoutException : DeviceResetterErroredOutException
    {
        public DeviceResetTimeoutException(int timeoutInMs) : base($"Failed to reset/reboot the device within {timeoutInMs}ms")
        {
        }
    }
}