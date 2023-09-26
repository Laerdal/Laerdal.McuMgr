using System;

namespace Laerdal.McuMgr.FirmwareInstaller.Contracts.Exceptions
{
    public class AllFirmwareInstallationAttemptsFailedException : FirmwareInstallationErroredOutException, IFirmwareInstallationException
    {
        public AllFirmwareInstallationAttemptsFailedException(int maxRetriesCount, Exception innerException = null)
            : base($"Failed to install firmware after trying {maxRetriesCount + 1} time(s)", innerException)
        {
        }
        
        public AllFirmwareInstallationAttemptsFailedException(string errorMessage)
            : base($"An error occurred while uploading the requested resource: '{errorMessage}'")
        {
        }
        
        public AllFirmwareInstallationAttemptsFailedException(string errorMessage, Exception innerException)
            : base($"An error occurred while uploading the requested resource: '{errorMessage}'", innerException)
        {
        }
    }
}