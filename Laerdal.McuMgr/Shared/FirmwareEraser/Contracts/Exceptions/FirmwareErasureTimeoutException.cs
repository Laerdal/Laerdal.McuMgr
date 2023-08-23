using System;

namespace Laerdal.McuMgr.FirmwareEraser.Contracts.Exceptions
{
    public sealed class FirmwareErasureTimeoutException : FirmwareErasureErroredOutException, IFirmwareEraserException
    {
        public FirmwareErasureTimeoutException(int timeoutInMs, Exception innerException)
            : base($"Failed to erase firmware on the device within {timeoutInMs}ms", innerException)
        {
        }
    }
}