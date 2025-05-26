using Laerdal.McuMgr.Common.Events;

namespace Laerdal.McuMgr.FileUploader.Contracts.Events
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