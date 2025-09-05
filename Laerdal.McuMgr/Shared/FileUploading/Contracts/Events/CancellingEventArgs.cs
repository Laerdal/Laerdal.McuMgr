using Laerdal.McuMgr.Common.Events;

namespace Laerdal.McuMgr.FileUploading.Contracts.Events
{
    public readonly struct CancellingEventArgs : IMcuMgrEventArgs
    {
        public string Reason { get; init; }
        
        public CancellingEventArgs(string reason)
        {
            Reason = reason;
        }
    }
}