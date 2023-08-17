using System;

namespace Laerdal.McuMgr.FileUploader.Contracts.Exceptions
{
    public class UploadCancelledException : Exception, IUploadRelatedException
    {
        public UploadCancelledException() : base("Upload was cancelled")
        {
        }
    }
}