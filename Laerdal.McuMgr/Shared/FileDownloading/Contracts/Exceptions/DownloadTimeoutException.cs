// ReSharper disable RedundantExtendsListEntry

using System;

namespace Laerdal.McuMgr.FileDownloading.Contracts.Exceptions
{
    public sealed class DownloadTimeoutException : DownloadErroredOutException, IDownloadException
    {
        public DownloadTimeoutException(string remoteFilePath, int timeoutInMs, Exception innerException)
            : base($"Failed to download '{remoteFilePath}' from the device within {timeoutInMs}ms", innerException)
        {
        }
    }
}
