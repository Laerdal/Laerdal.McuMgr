using Laerdal.McuMgr.Common.Events;

namespace Laerdal.McuMgr.FileUploading.Contracts.Events
{
    public readonly struct FileUploadStartedEventArgs : IMcuMgrEventArgs //hotpathish
    {
        public readonly string ResourceId;
        public readonly string RemoteFilePath;
        public readonly long TotalBytesToBeUploaded;

        public FileUploadStartedEventArgs(string resourceId, string remoteFilePath, long totalBytesToBeUploaded)
        {
            ResourceId = resourceId;
            RemoteFilePath = remoteFilePath;
            TotalBytesToBeUploaded = totalBytesToBeUploaded;
        }
    }
}