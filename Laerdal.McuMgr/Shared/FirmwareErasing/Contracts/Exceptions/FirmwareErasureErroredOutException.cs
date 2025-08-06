using System;
using Laerdal.McuMgr.Common.Enums;

namespace Laerdal.McuMgr.FirmwareErasure.Contracts.Exceptions
{
    public class FirmwareErasureErroredOutException : Exception, IFirmwareEraserException
    {
        public EGlobalErrorCode GlobalErrorCode { get; }

        public FirmwareErasureErroredOutException(string errorMessage, EGlobalErrorCode globalErrorCode) : base($"An error occurred while erasing firmware: '{errorMessage}'")
        {
            GlobalErrorCode = globalErrorCode;
        }
        
        public FirmwareErasureErroredOutException(string errorMessage, Exception innerException) : base($"An error occurred while erasing firmware: '{errorMessage}'", innerException)
        {
        }
    }
}