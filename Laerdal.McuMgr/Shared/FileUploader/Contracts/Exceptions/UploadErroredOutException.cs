using System;

namespace Laerdal.McuMgr.FileUploader.Contracts.Exceptions
{
    public class UploadErroredOutException : Exception, IUploadException
    {
        public UploadErroredOutException(string errorMessage)
            : base($"An error occurred while uploading the requested resource: '{errorMessage}'")
        {
        }
        
        public UploadErroredOutException(string errorMessage, Exception innerException)
            : base($"An error occurred while uploading the requested resource: '{errorMessage}'", innerException)
        {
        }
    }
}