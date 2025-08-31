using System;

namespace Laerdal.McuMgr.FileDownloading
{
    public partial class FileDownloader
    {
        private bool _disposed;
        public void Dispose()
        {
            Dispose(isDisposing: true);

            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool isDisposing)
        {
            if (_disposed)
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

            _disposed = true;
        }
    }
}