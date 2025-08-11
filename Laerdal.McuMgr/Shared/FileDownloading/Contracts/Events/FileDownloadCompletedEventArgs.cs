using Laerdal.McuMgr.Common.Events;

namespace Laerdal.McuMgr.FileDownloading.Contracts.Events
{
    public readonly struct FileDownloadCompletedEventArgs : IMcuMgrEventArgs //hotpathish
    {
        public readonly byte[] Data;
        public readonly string ResourceId; //remote file path essentially

        public FileDownloadCompletedEventArgs(string resourceId, byte[] data)
        {
            Data = data;
            ResourceId = resourceId;
        }
    }
}