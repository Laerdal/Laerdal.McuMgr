using System;

namespace Laerdal.McuMgr.FileUploader.Exceptions
{
    public class UploadCancelledException : Exception, IUploadRelatedException
    {
        public UploadCancelledException() : base("Upload was cancelled")
        {
        }
    }
}