// ReSharper disable UnusedType.Global
// ReSharper disable RedundantExtendsListEntry

using System;

namespace Laerdal.McuMgr.FileUploading
{
    public partial class FileUploader
    {
        protected bool IsDisposed;
        public void Dispose()
        {
            Dispose(isDisposing: true);

            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool isDisposing)
        {
            if (IsDisposed)
                return;

            if (!isDisposing)
                return;

            KeepGoing.Set(); // unblock any pause to let it observe the disposal
            
            try
            {
                NativeFileUploaderProxy?.Dispose();
            }
            catch
            {
                //ignored
            }

            IsDisposed = true;
        }
    }
}
