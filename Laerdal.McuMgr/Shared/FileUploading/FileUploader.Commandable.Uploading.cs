using System;

namespace Laerdal.McuMgr.FileUploading
{
    public partial class FileUploader
    {
        protected void EnsureExclusiveOperationToken()
        {
            if (IsDisposed)
                throw new ObjectDisposedException(nameof(FileUploader));
            
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
