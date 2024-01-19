using System;

namespace Laerdal.McuMgr.FileDownloader.Contracts.Exceptions
{
    public class AllDownloadAttemptsFailedException : DownloadErroredOutException, IDownloadException
    {
        public AllDownloadAttemptsFailedException(string remoteFilePath, int triesCount, Exception innerException = null)
            : base($"Failed to download '{remoteFilePath}' after trying {triesCount} time(s)", innerException)
        {
        }
        
        public AllDownloadAttemptsFailedException(string errorMessage)
            : base($"An error occurred while downloading the requested resource: '{errorMessage}'")
        {
        }
        
        public AllDownloadAttemptsFailedException(string errorMessage, Exception innerException)
            : base($"An error occurred while downloading the requested resource: '{errorMessage}'", innerException)
        {
        }
    }
}