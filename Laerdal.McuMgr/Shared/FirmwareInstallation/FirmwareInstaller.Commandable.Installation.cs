using System;
using Laerdal.McuMgr.FirmwareInstallation.Contracts.Exceptions;

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
                    throw new AnotherFirmwareInstallationIsAlreadyOngoingException();

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
