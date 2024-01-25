using Laerdal.McuMgr.Common.Events;

namespace Laerdal.McuMgr.FileUploader.Contracts.Events
{
    public readonly struct FileUploadedEventArgs : IMcuMgrEventArgs
    {
        public string Resource { get; }

        public FileUploadedEventArgs(string resource)
        {
            Resource = resource;
        }
    }
}