using System;

namespace Laerdal.McuMgr.FirmwareInstallation.Contracts.Exceptions
{
    public class FirmwareInstallationInternalErrorException : Exception, IFirmwareInstallationException
    {
        public FirmwareInstallationInternalErrorException(string message = "(no details available)", Exception innerException = null)
            : base($"An internal error occured - report what you did to reproduce this because this is most probably a bug: {message}", innerException)
        {
        }
    }
}