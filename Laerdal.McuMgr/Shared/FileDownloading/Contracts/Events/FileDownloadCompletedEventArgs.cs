using Laerdal.McuMgr.Common.Events;

namespace Laerdal.McuMgr.FileDownloading.Contracts.Events
{
    public readonly struct FileDownloadCompletedEventArgs : IMcuMgrEventArgs //hotpathish
    {
        public readonly byte[] Data;
        public readonly string RemoteFilePath; //remote file path essentially

        public FileDownloadCompletedEventArgs(string remoteFilePath, byte[] data)
        {
            Data = data;
            RemoteFilePath = remoteFilePath;
        }
    }
}