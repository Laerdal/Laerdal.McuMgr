using System;

namespace Laerdal.McuMgr.FileDownloading.Contracts.Exceptions
{
    public class DownloadCancelledException : OperationCanceledException, IDownloadException
    {
        public DownloadCancelledException() : base("Download was cancelled")
        {
        }
    }
}