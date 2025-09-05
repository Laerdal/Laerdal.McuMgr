using System;
using Laerdal.McuMgr.Common.Events;

namespace Laerdal.McuMgr.FileDownloading.Contracts.Events
{
    public readonly struct CancellingEventArgs : IMcuMgrEventArgs
    {
        public readonly string Reason;
        
        public CancellingEventArgs(string reason)
        {
            Reason = reason ?? throw new ArgumentNullException(nameof(reason), "Reason cannot be null");
        }
    }
}