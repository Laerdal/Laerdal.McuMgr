using Laerdal.McuMgr.Common.Events;

namespace Laerdal.McuMgr.FileDownloading.Contracts.Events
{
    public readonly struct FileDownloadStartedEventArgs : IMcuMgrEventArgs //hotpathish
    {
        public readonly string ResourceId; //remote file path essentially

        public FileDownloadStartedEventArgs(string resourceId)
        {
            ResourceId = resourceId;
        }
    }
}