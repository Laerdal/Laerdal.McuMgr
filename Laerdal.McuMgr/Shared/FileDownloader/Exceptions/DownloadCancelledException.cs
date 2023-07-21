using System;

namespace Laerdal.McuMgr.FileDownloader.Exceptions
{
    public class DownloadCancelledException : Exception
    {
        public DownloadCancelledException() : base("Download was cancelled")
        {
        }
    }
}