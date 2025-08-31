using System;

namespace Laerdal.McuMgr.FileUploading
{
    public partial class FileUploader
    {
        protected void EnsureExclusiveOperation()
        {
            lock (OperationCheckLock)
            {
                if (IsOperationOngoing)
                    throw new InvalidOperationException("An upload operation is already running - cannot start another one");

                IsOperationOngoing = true;
            }
        }

        protected void ReleaseExclusiveOperation()
        {
            lock (OperationCheckLock)
            {
                IsOperationOngoing = false;
            }
        }
    }
}
