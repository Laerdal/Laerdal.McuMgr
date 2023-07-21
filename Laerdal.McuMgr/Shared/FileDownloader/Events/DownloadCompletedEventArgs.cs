using System;

namespace Laerdal.McuMgr.FileDownloader.Events
{
    public sealed class DownloadCompletedEventArgs : EventArgs
    {
        public byte[] Data { get; }

        public DownloadCompletedEventArgs(byte[] data)
        {
            Data = data;
        }
    }
}