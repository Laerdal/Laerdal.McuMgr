// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable ClassNeverInstantiated.Global

using System;
using Laerdal.McuMgr.Common.Events;

namespace Laerdal.McuMgr.FileDownloading.Contracts.Events
{
    public readonly struct CancelledEventArgs : IMcuMgrEventArgs
    {
        public readonly string Reason;
        
        public CancelledEventArgs(string reason)
        {
            Reason = reason ?? throw new ArgumentNullException(nameof(reason), "Reason cannot be null");
        }
    }
}