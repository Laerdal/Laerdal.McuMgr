using System;

namespace Laerdal.McuMgr.FileDownloader.Exceptions
{
    public class DownloadErroredOutException : Exception
    {
        public DownloadErroredOutException(string errorMessage) : base($"An error occurred while downloading the requested resource: '{errorMessage}'")
        {
        }
        
        public DownloadErroredOutException(string errorMessage, Exception innerException) : base($"An error occurred while downloading the requested resource: '{errorMessage}'", innerException)
        {
        }
    }
}