using System;

namespace Laerdal.McuMgr.FileDownloader.Contracts.Exceptions
{
    public class DownloadCancelledException : OperationCanceledException, IDownloadException
    {
        public DownloadCancelledException() : base("Download was cancelled")
        {
        }
    }
}