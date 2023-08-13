using System;

namespace Laerdal.McuMgr.FileDownloader.Contracts.Exceptions
{
    public class DownloadCancelledException : Exception
    {
        public DownloadCancelledException() : base("Download was cancelled")
        {
        }
    }
}