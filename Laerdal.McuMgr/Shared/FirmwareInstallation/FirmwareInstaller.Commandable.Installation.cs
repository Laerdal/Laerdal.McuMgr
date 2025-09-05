using System;
using System.Threading.Tasks;
using Laerdal.McuMgr.FirmwareInstallation.Contracts.Exceptions;

namespace Laerdal.McuMgr.FirmwareInstallation
{
    public partial class FirmwareInstaller
    {
        protected virtual async Task EnsureExclusiveOperationTokenAsync() //00 async
        {
            if (IsDisposed)
                throw new ObjectDisposedException(nameof(FirmwareInstaller));
            
            lock (OperationCheckLock)
            {
                if (IsOperationOngoing)
                    throw new AnotherFirmwareInstallationIsAlreadyOngoingException();

                IsOperationOngoing = true;
            }
            
            await Task.CompletedTask; // just to make the compiler happy
            
            //00 made async to allow for future overrides or future iterations that might need to actually await something
        }

        protected virtual async Task ReleaseExclusiveOperationTokenAsync() //00 async
        {
            lock (OperationCheckLock)
            {
                IsOperationOngoing = false;
            }
            
            await Task.CompletedTask;
            
            //00 made async to allow for future overrides or future iterations that might need to actually await something
        }
    }
}
