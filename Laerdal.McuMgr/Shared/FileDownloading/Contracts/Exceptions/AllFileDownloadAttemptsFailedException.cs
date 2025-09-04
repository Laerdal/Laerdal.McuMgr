using System;

namespace Laerdal.McuMgr.FileDownloading.Contracts.Exceptions
{
    public class AllFileDownloadAttemptsFailedException : FileDownloadErroredOutException, IDownloadException
    {
        public AllFileDownloadAttemptsFailedException(string remoteFilePath, int triesCount, Exception innerException = null)
            : base($"Failed to download '{remoteFilePath}' after trying {triesCount} time(s)", innerException)
        {
        }
        
        public AllFileDownloadAttemptsFailedException(string errorMessage)
            : base($"An error occurred while downloading the requested resource: '{errorMessage}'")
        {
        }
        
        public AllFileDownloadAttemptsFailedException(string errorMessage, Exception innerException)
            : base($"An error occurred while downloading the requested resource: '{errorMessage}'", innerException)
        {
        }
    }
}
