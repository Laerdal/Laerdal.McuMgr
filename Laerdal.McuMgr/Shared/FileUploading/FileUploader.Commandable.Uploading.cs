using System;
using System.Threading.Tasks;

namespace Laerdal.McuMgr.FileUploading
{
    public partial class FileUploader
    {
        protected Task EnsureExclusiveOperationTokenAsync()
        {
            if (IsDisposed)
                throw new ObjectDisposedException(nameof(FileUploader));
            
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
