using Laerdal.McuMgr.Common.Events;

namespace Laerdal.McuMgr.FileDownloading.Contracts.Events
{
    public readonly struct FileDownloadPausedEventArgs : IMcuMgrEventArgs
    {
        public readonly string RemoteFilePath;

        public FileDownloadPausedEventArgs(string remoteFilePath)
        {
            RemoteFilePath = remoteFilePath;
        }
    }
}