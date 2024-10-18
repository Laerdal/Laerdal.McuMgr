using System;

namespace Laerdal.McuMgr.FileUploader.Contracts.Exceptions
{
    public class UploadCancelledException : Exception, IUploadException
    {
        public UploadCancelledException(string optionalReason = "")
            : base($"Upload was cancelled{(string.IsNullOrWhiteSpace(optionalReason) ? "" : $": {optionalReason}")}")
        {
        }
    }
}
