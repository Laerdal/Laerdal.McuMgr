using System;

namespace Laerdal.McuMgr.FileDownloader.Contracts.Exceptions
{
    public class DownloadCancelledException : Exception, IDownloadRelatedException
    {
        public DownloadCancelledException() : base("Download was cancelled")
        {
        }
    }
}