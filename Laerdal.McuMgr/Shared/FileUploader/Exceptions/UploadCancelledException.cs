using System;

namespace Laerdal.McuMgr.FileUploader.Exceptions
{
    public class UploadCancelledException : Exception
    {
        public UploadCancelledException() : base("Upload was cancelled")
        {
        }
    }
}