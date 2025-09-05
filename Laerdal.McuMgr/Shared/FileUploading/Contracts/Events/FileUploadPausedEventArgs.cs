using Laerdal.McuMgr.Common.Events;

namespace Laerdal.McuMgr.FileUploading.Contracts.Events
{
    public readonly struct FileUploadPausedEventArgs : IMcuMgrEventArgs
    {
        public readonly string ResourceId;
        public readonly string RemoteFilePath;

        public FileUploadPausedEventArgs(string resourceId, string remoteFilePath)
        {
            ResourceId = resourceId;
            RemoteFilePath = remoteFilePath;
        }
    }
}