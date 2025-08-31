// ReSharper disable UnusedType.Global
// ReSharper disable RedundantExtendsListEntry

using Laerdal.McuMgr.Common.Enums;
using Laerdal.McuMgr.Common.Events;

namespace Laerdal.McuMgr.FileUploading
{
    public partial class FileUploader
    {
        public bool TryResume()
        {
            if (IsDisposed || IsCancellationRequested || !IsOperationOngoing)
                return false;

            OnLogEmitted(new LogEmittedEventArgs(level: ELogLevel.Trace, message: "[FU.TRS.010] Received request to resume the upload operation (if any)", category: "FileUploader", resource: ""));

            KeepGoing.Set(); //order                         unblocks any ongoing installation/verification
            NativeFileUploaderProxy?.TryResume(); //order    ignore the return value

            return true; //must always return true
        }
    }
}
