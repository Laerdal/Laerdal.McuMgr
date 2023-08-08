using System;

namespace Laerdal.McuMgr.DeviceResetter.Exceptions
{
    public sealed class DeviceResetterErroredOutException : Exception
    {
        public DeviceResetterErroredOutException(string errorMessage) : base($"An error occurred while resetting/rebooting the device: '{errorMessage}'")
        {
        }
        
        public DeviceResetterErroredOutException(string errorMessage, Exception innerException) : base($"An error occurred while resetting/rebooting the device: '{errorMessage}'", innerException)
        {
        }
    }
}