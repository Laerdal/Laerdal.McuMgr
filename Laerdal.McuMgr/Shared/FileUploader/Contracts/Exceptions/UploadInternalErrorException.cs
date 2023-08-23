using System;

namespace Laerdal.McuMgr.FileUploader.Contracts.Exceptions
{
    public class UploadInternalErrorException : UploadErroredOutException, IUploadRelatedException
    {
        public UploadInternalErrorException(Exception innerException = null)
            : base($"An internal error occured (this is most probably a bug)", innerException)
        {
        }
    }
}