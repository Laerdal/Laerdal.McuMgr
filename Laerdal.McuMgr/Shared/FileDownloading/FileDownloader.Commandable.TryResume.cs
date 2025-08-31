using Laerdal.McuMgr.Common.Enums;
using Laerdal.McuMgr.Common.Events;

namespace Laerdal.McuMgr.FileDownloading
{
    public partial class FileDownloader
    {
        public bool TryResume()
        {
            if (IsDisposed || IsCancellationRequested || !IsOperationOngoing)
                return false;

            OnLogEmitted(new LogEmittedEventArgs(level: ELogLevel.Trace, message: "[FD.TRS.010] Received request to resume the download operation (if any)", category: "FileDownloader", resource: ""));

            KeepGoing.Set(); //order                           unblocks any ongoing installation/verification
            NativeFileDownloaderProxy?.TryResume(); //order    ignore the return value

            return true; //must always return true
        }
    }
}