using System;
using Laerdal.McuMgr.Common.Enums;

namespace Laerdal.McuMgr.DeviceResetting.Contracts.Exceptions
{
    public class DeviceResetterErroredOutException : Exception, IDeviceResetterException
    {
        public EGlobalErrorCode GlobalErrorCode { get; } = EGlobalErrorCode.Unset;

        public DeviceResetterErroredOutException(string errorMessage, EGlobalErrorCode globalErrorCode) : base($"An error occurred while resetting/rebooting the device: '{errorMessage}'")
        {
            GlobalErrorCode = globalErrorCode;
        }
        
        public DeviceResetterErroredOutException(string errorMessage, Exception innerException) : base($"An error occurred while resetting/rebooting the device: '{errorMessage}'", innerException)
        {
        }
    }
}