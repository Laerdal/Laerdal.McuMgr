using System;

namespace Laerdal.McuMgr.FileDownloader.Contracts.Exceptions
{
    public class DownloadCancelledException : Exception, IDownloadException
    {
        public DownloadCancelledException() : base("Download was cancelled")
        {
        }
    }
}