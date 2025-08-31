using System;

namespace Laerdal.McuMgr.FileDownloading
{
    public partial class FileDownloader
    {
        protected void EnsureExclusiveOperationToken()
        {
            if (IsDisposed)
                throw new ObjectDisposedException(nameof(FileDownloader));
            
            lock (OperationCheckLock)
            {
                if (IsOperationOngoing)
                    throw new InvalidOperationException("An upload operation is already running - cannot start another one");

                IsOperationOngoing = true;
            }
        }

        protected void ReleaseExclusiveOperationToken()
        {
            lock (OperationCheckLock)
            {
                IsOperationOngoing = false;
            }
        }
    }
}