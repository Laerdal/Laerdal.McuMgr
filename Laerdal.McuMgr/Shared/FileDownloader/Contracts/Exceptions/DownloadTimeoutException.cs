using System;

namespace Laerdal.McuMgr.FileDownloader.Contracts.Exceptions
{
    public sealed class DownloadTimeoutException : DownloadErroredOutException
    {
        public DownloadTimeoutException(string remoteFilePath, int timeoutInMs, Exception innerException)
            : base($"Failed to download '{remoteFilePath}' from the device within {timeoutInMs}ms", innerException)
        {
        }
    }
}