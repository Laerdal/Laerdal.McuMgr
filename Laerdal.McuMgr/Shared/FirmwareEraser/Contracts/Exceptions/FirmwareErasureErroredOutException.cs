using System;

namespace Laerdal.McuMgr.FirmwareEraser.Contracts.Exceptions
{
    public sealed class FirmwareErasureErroredOutException : Exception
    {
        public FirmwareErasureErroredOutException(string errorMessage) : base($"An error occurred while erasing firmware: '{errorMessage}'")
        {
        }
        
        public FirmwareErasureErroredOutException(string errorMessage, Exception innerException) : base($"An error occurred while erasing firmware: '{errorMessage}'", innerException)
        {
        }
    }
}