using System;

namespace Laerdal.McuMgr.FileUploader.Contracts.Exceptions
{
    public class AllUploadAttemptsFailedException : UploadErroredOutException, IUploadException
    {
        public AllUploadAttemptsFailedException(string remoteFilePath, int maxRetriesCount, Exception innerException = null)
            : base($"Failed to upload '{remoteFilePath}' after trying {maxRetriesCount + 1} time(s)", innerException)
        {
        }
        
        public AllUploadAttemptsFailedException(string errorMessage)
            : base($"An error occurred while uploading the requested resource: '{errorMessage}'")
        {
        }
        
        public AllUploadAttemptsFailedException(string errorMessage, Exception innerException)
            : base($"An error occurred while uploading the requested resource: '{errorMessage}'", innerException)
        {
        }
    }
}
