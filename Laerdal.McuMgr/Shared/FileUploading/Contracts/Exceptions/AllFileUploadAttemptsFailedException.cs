using System;

namespace Laerdal.McuMgr.FileUploading.Contracts.Exceptions
{
    public class AllFileUploadAttemptsFailedException : FileUploadErroredOutException, IUploadException
    {
        public AllFileUploadAttemptsFailedException(string remoteFilePath, int triesCount, Exception innerException = null)
            : base(remoteFilePath, $"Failed to upload '{remoteFilePath}' after trying {triesCount} time(s)", innerException: innerException)
        {
        }
    }
}
