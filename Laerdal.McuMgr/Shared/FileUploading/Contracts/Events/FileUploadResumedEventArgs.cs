using Laerdal.McuMgr.Common.Events;

namespace Laerdal.McuMgr.FileUploading.Contracts.Events
{
    public readonly struct FileUploadResumedEventArgs : IMcuMgrEventArgs //hotpathish
    {
        public readonly string ResourceId;
        public readonly string RemoteFilePath;

        public FileUploadResumedEventArgs(string resourceId, string remoteFilePath)
        {
            ResourceId = resourceId;
            RemoteFilePath = remoteFilePath;
        }
    }
}