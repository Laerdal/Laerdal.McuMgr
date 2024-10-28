using System;
using Laerdal.McuMgr.Common.Enums;
using Laerdal.McuMgr.FileUploader.Contracts.Enums;

namespace Laerdal.McuMgr.FileUploader.Contracts.Exceptions
{
    public class UploadErroredOutException : Exception, IUploadException
    {
        public string RemoteFilePath { get; }
        
        public EGlobalErrorCode GlobalErrorCode { get; } = EGlobalErrorCode.Unset;

        protected UploadErroredOutException(string remoteFilePath, string errorMessage, Exception innerException = null)
            : base($"An error occurred while uploading over to '{remoteFilePath}': '{errorMessage}'", innerException)
        {
            RemoteFilePath = remoteFilePath;
        }

        public UploadErroredOutException(
            string nativeErrorMessage,
            string remoteFilePath,
            EGlobalErrorCode globalErrorCode,
            Exception innerException = null
        ) : base($"An error occurred while uploading '{remoteFilePath}': '{nativeErrorMessage}' (globalErrorCode={globalErrorCode})", innerException)
        {
            RemoteFilePath = remoteFilePath;
            GlobalErrorCode = globalErrorCode;
        }
    }
}
