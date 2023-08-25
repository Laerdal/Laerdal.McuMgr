using System;

namespace Laerdal.McuMgr.FirmwareInstaller.Contracts.Exceptions
{
    public class FirmwareInstallationInternalErrorException : Exception, IFirmwareInstallationException
    {
        public FirmwareInstallationInternalErrorException(Exception innerException = null)
            : base("An internal error occured - report what you did to reproduce this because this is most probably a bug!", innerException)
        {
        }
    }
}