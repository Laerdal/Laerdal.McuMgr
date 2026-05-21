using System;

namespace Laerdal.McuMgr.FileDownloading.Contracts.Exceptions
{
    public class DownloadCancelledException : OperationCanceledException, IDownloadException
    {
        public DownloadCancelledException(string cancellationReason) : base($"Download was cancelled: {cancellationReason}")
        {
        }
    }
}