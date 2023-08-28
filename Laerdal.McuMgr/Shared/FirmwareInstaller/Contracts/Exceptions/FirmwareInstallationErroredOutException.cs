using System;

namespace Laerdal.McuMgr.FirmwareInstaller.Contracts.Exceptions
{
    public class FirmwareInstallationErroredOutException : Exception, IFirmwareInstallationException
    {
        public FirmwareInstallationErroredOutException(string errorMessage) : base($"An error occurred during firmware installation: '{errorMessage}'")
        {
        }
        
        public FirmwareInstallationErroredOutException(string errorMessage, Exception innerException) : base($"An error occurred during firmware installation: '{errorMessage}'", innerException)
        {
        }
    }
}