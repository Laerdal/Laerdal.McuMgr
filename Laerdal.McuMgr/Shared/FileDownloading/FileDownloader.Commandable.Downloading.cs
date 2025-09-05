using System;
using System.Threading.Tasks;

namespace Laerdal.McuMgr.FileDownloading
{
    public partial class FileDownloader
    {
        protected Task EnsureExclusiveOperationTokenAsync()
        {
            if (IsDisposed)
                throw new ObjectDisposedException(nameof(FileDownloader));
            
            lock (OperationCheckLock)
            {
                if (IsOperationOngoing)
                    throw new InvalidOperationException("An upload operation is already running - cannot start another one");

                IsOperationOngoing = true;
            }
            
            return Task.CompletedTask;
        }

        protected Task ReleaseExclusiveOperationTokenAsync()
        {
            lock (OperationCheckLock)
            {
                IsOperationOngoing = false;
            }
            
            return Task.CompletedTask;
        }
    }
}