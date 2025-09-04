using System;

namespace Laerdal.McuMgr.FileDownloading.Contracts.Exceptions
{
    // ReSharper disable once RedundantExtendsListEntry
    public class DownloadInternalErrorException : DownloadErroredOutException, IDownloadException
    {
        public DownloadInternalErrorException(string message = "(no details)", Exception innerException = null)
            : base($"An internal error occured - share the logs and report what you did to reproduce this because this is most probably a bug: {message}", innerException)
        {
        }
    }
}