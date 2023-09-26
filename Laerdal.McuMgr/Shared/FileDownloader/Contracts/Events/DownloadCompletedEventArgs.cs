using Laerdal.McuMgr.Common.Events;

namespace Laerdal.McuMgr.FileDownloader.Contracts.Events
{
    public readonly struct DownloadCompletedEventArgs : IMcuMgrEventArgs
    {
        public byte[] Data { get; }
        public string Resource { get; } //remote file path essentially

        public DownloadCompletedEventArgs(string resource, byte[] data)
        {
            Data = data;
            Resource = resource;
        }
    }
}