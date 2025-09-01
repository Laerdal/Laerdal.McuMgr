// ReSharper disable UnusedType.Global
// ReSharper disable RedundantExtendsListEntry

using System;

namespace Laerdal.McuMgr.FirmwareInstallation
{
    public partial class FirmwareInstaller
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
                _nativeFirmwareInstallerProxy?.Dispose();
            }
            catch
            {
                //ignored
            }

            _disposed = true;
        }
    }
}