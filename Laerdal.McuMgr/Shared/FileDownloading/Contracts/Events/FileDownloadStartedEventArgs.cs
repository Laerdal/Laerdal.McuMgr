using Laerdal.McuMgr.Common.Events;

namespace Laerdal.McuMgr.FileDownloading.Contracts.Events
{
    public readonly struct FileDownloadStartedEventArgs : IMcuMgrEventArgs //hotpathish
    {
        public readonly long TotalBytesToBeDownloaded;
        public readonly string RemoteFilePath;

        public FileDownloadStartedEventArgs(string remoteFilePath, long totalBytesToBeDownloaded)
        {
            RemoteFilePath = remoteFilePath;
            TotalBytesToBeDownloaded = totalBytesToBeDownloaded;
        }
    }
}