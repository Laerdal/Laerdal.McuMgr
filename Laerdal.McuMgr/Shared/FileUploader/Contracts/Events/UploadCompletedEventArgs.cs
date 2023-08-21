using System;

namespace Laerdal.McuMgr.FileUploader.Contracts.Events
{
    public sealed class UploadCompletedEventArgs : EventArgs
    {
        public string Resource { get; }

        public UploadCompletedEventArgs(string resource)
        {
            Resource = resource;
        }
    }
}