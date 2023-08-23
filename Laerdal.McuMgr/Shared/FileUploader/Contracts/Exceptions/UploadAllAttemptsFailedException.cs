using System;

namespace Laerdal.McuMgr.FileUploader.Contracts.Exceptions
{
    public class UploadAllAttemptsFailedException : UploadErroredOutException, IUploadRelatedException
    {
        public UploadAllAttemptsFailedException(string remoteFilePath, int maxRetriesCount, Exception innerException = null)
            : base($"Failed to upload '{remoteFilePath}' after trying {maxRetriesCount + 1} time(s)", innerException)
        {
        }
        
        public UploadAllAttemptsFailedException(string errorMessage)
            : base($"An error occurred while uploading the requested resource: '{errorMessage}'")
        {
        }
        
        public UploadAllAttemptsFailedException(string errorMessage, Exception innerException)
            : base($"An error occurred while uploading the requested resource: '{errorMessage}'", innerException)
        {
        }
    }
}
