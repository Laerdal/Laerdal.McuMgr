using System;

namespace Laerdal.McuMgr.FirmwareEraser.Contracts.Exceptions
{
    public sealed class FirmwareErasureErroredOutException : Exception
    {
        public FirmwareErasureErroredOutException(string errorMessage) : base($"An error occurred while performing erasure: '{errorMessage}'")
        {
        }
        
        public FirmwareErasureErroredOutException(string errorMessage, Exception exception) : base($"An error occurred while performing erasure: '{errorMessage}'", exception)
        {
        }
    }
}