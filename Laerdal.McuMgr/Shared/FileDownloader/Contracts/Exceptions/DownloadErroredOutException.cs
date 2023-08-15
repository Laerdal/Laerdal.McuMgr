using System;

namespace Laerdal.McuMgr.FileDownloader.Contracts.Exceptions
{
    public class DownloadErroredOutException : Exception, IDownloadRelatedException
    {
        public DownloadErroredOutException(string errorMessage) : base($"An error occurred while downloading the requested resource: '{errorMessage}'")
        {
        }
        
        public DownloadErroredOutException(string errorMessage, Exception innerException) : base($"An error occurred while downloading the requested resource: '{errorMessage}'", innerException)
        {
        }
    }
}