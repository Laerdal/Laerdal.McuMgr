using Laerdal.McuMgr.Common.Events;

namespace Laerdal.McuMgr.FileDownloading.Contracts.Events
{
    public readonly struct DownloadCompletedEventArgs : IMcuMgrEventArgs //hotpathish
    {
        public readonly byte[] Data;
        public readonly string Resource; //remote file path essentially

        public DownloadCompletedEventArgs(string resource, byte[] data)
        {
            Data = data;
            Resource = resource;
        }
    }
}