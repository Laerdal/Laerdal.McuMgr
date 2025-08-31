using System;

namespace Laerdal.McuMgr.FileDownloading
{
    public partial class FileDownloader
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

            try
            {
                NativeFileDownloaderProxy?.Dispose();
            }
            catch
            {
                //ignored
            }

            IsDisposed = true;
        }
    }
}