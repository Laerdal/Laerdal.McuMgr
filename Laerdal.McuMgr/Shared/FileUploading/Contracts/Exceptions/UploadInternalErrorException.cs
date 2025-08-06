using System;

namespace Laerdal.McuMgr.FileUploading.Contracts.Exceptions
{
    public class UploadInternalErrorException : UploadErroredOutException, IUploadException
    {
        public UploadInternalErrorException(string remoteFilePath, Exception innerException = null)
            : base(remoteFilePath, "An internal error occured - report what you did to reproduce this because this is most probably a bug!", innerException: innerException)
        {
        }
    }
}
