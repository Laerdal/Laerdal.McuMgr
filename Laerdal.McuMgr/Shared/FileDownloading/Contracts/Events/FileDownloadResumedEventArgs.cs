using Laerdal.McuMgr.Common.Events;

namespace Laerdal.McuMgr.FileDownloading.Contracts.Events
{
    public readonly struct FileDownloadResumedEventArgs : IMcuMgrEventArgs
    {
        public readonly string RemoteFilePath;

        public FileDownloadResumedEventArgs(string remoteFilePath)
        {
            RemoteFilePath = remoteFilePath;
        }
    }
}