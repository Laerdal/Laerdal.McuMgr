using System;

namespace Laerdal.McuMgr.FileUploader.Contracts.Events
{
    public sealed class UploadCompletedEventArgs : EventArgs
    {
        public byte[] Data { get; }
        public string Resource { get; }

        public UploadCompletedEventArgs(string resource, byte[] data)
        {
            Data = data;
            Resource = resource;
        }
    }
}