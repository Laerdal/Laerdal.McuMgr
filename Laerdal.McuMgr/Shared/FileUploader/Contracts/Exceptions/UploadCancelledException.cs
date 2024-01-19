using System;

namespace Laerdal.McuMgr.FileUploader.Contracts.Exceptions
{
    public class UploadCancelledException : Exception, IUploadException
    {
        public UploadCancelledException() : base("Upload was cancelled")
        {
        }
    }
}
