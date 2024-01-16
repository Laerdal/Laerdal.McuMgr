using System;

namespace Laerdal.McuMgr.FileUploader.Contracts.Exceptions
{
    public class AllUploadAttemptsFailedException : UploadErroredOutException, IUploadException
    {
        public AllUploadAttemptsFailedException(string remoteFilePath, int maxRetriesCount, Exception innerException = null)
            : base(remoteFilePath, $"Failed to upload '{remoteFilePath}' after trying {maxRetriesCount + 1} time(s)", innerException)
        {
        }
    }
}
