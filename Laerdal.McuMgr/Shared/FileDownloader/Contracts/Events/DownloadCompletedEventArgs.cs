using System;

namespace Laerdal.McuMgr.FileDownloader.Contracts.Events
{
    public sealed class DownloadCompletedEventArgs : EventArgs
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