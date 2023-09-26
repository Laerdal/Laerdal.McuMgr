using System;

namespace Laerdal.McuMgr.FirmwareEraser.Contracts.Exceptions
{
    public class FirmwareErasureErroredOutException : Exception, IFirmwareEraserException
    {
        public FirmwareErasureErroredOutException(string errorMessage) : base($"An error occurred while erasing firmware: '{errorMessage}'")
        {
        }
        
        public FirmwareErasureErroredOutException(string errorMessage, Exception innerException) : base($"An error occurred while erasing firmware: '{errorMessage}'", innerException)
        {
        }
    }
}