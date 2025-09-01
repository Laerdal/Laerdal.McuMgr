using System;

namespace Laerdal.McuMgr.FirmwareInstallation
{
    public partial class FirmwareInstaller
    {
        protected virtual void EnsureExclusiveOperationToken()
        {
            if (IsDisposed)
                throw new ObjectDisposedException(nameof(FirmwareInstaller));
            
            lock (OperationCheckLock)
            {
                if (IsOperationOngoing)
                    throw new InvalidOperationException("An firmware-installation operation is already running - cannot start another one");

                IsOperationOngoing = true;
            }
        }

        protected virtual void ReleaseExclusiveOperationToken()
        {
            lock (OperationCheckLock)
            {
                IsOperationOngoing = false;
            }
        }
    }
}
