using System;

namespace Laerdal.McuMgr.FirmwareEraser.Contracts.Exceptions
{
    public class FirmwareErasureInternalErrorException : FirmwareErasureErroredOutException, IFirmwareEraserException
    {
        public FirmwareErasureInternalErrorException(Exception innerException = null)
            : base("An internal error occured - report what you did to reproduce this because this is most probably a bug!", innerException)
        {
        }
    }
}