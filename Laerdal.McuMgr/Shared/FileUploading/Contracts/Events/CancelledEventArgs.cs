// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable ClassNeverInstantiated.Global

using Laerdal.McuMgr.Common.Events;

namespace Laerdal.McuMgr.FileUploading.Contracts.Events
{
    public readonly struct CancelledEventArgs : IMcuMgrEventArgs
    {
        public string Reason { get; init; }
        
        public CancelledEventArgs(string reason)
        {
            Reason = reason;
        }
    }
}