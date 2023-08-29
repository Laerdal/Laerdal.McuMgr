using Laerdal.McuMgr.Common.Events;

namespace Laerdal.McuMgr.FileUploader.Contracts.Events
{
    public readonly struct UploadCompletedEventArgs : IMcuMgrEventArgs
    {
        public string Resource { get; }

        public UploadCompletedEventArgs(string resource)
        {
            Resource = resource;
        }
    }
}