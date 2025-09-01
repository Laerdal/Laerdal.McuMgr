// ReSharper disable UnusedType.Global
// ReSharper disable RedundantExtendsListEntry

using System;

namespace Laerdal.McuMgr.FirmwareInstallation
{
    public partial class FirmwareInstaller
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
                _nativeFirmwareInstallerProxy?.Dispose();
            }
            catch
            {
                //ignored
            }

            IsDisposed = true;
        }
    }
}