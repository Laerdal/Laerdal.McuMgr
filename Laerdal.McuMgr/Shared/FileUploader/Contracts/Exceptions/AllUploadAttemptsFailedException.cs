using System;

namespace Laerdal.McuMgr.FileUploader.Contracts.Exceptions
{
    public class AllUploadAttemptsFailedException : UploadErroredOutException, IUploadException
    {
        public AllUploadAttemptsFailedException(string remoteFilePath, int triesCount, Exception innerException = null)
            : base(remoteFilePath, $"Failed to upload '{remoteFilePath}' after trying {triesCount} time(s)", innerException)
        {
        }
    }
}
