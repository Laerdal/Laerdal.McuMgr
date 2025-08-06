using System;

namespace Laerdal.McuMgr.DeviceResetting.Contracts.Exceptions
{
    public class DeviceResetterInternalErrorException : DeviceResetterErroredOutException, IDeviceResetterException
    {
        public DeviceResetterInternalErrorException(Exception innerException = null)
            : base("An internal error occured - report what you did to reproduce this because this is most probably a bug!", innerException)
        {
        }
    }
}