using System;

namespace Laerdal.McuMgr.FirmwareInstaller.Contracts.Exceptions
{
    public class FirmwareInstallationErroredOutException : Exception, IFirmwareInstallationException
    {
        public FirmwareInstallationErroredOutException(string errorMessage) : base($"An error occurred while performing the firmware installation: '{errorMessage}'")
        {
        }
        
        public FirmwareInstallationErroredOutException(string errorMessage, Exception innerException) : base($"An error occurred while performing the firmware installation: '{errorMessage}'", innerException)
        {
        }
    }
}