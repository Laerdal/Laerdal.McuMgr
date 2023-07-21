using System;

namespace Laerdal.McuMgr.FirmwareEraser.Exceptions
{
    public sealed class FirmwareErasureErroredOutException : Exception
    {
        public FirmwareErasureErroredOutException(string errorMessage) : base($"An error occurred while performing erasure: '{errorMessage}'")
        {
        }
    }
}