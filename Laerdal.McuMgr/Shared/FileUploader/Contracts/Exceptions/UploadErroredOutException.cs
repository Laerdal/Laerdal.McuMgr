using System;
using Laerdal.McuMgr.Common.Enums;

namespace Laerdal.McuMgr.FileUploader.Contracts.Exceptions
{
    public class UploadErroredOutException : Exception, IUploadException
    {
        public string RemoteFilePath { get; }
        public EGlobalErrorCode GlobalErrorCode { get; }

        public UploadErroredOutException(
            string nativeErrorMessage,
            string remoteFilePath,
            EGlobalErrorCode globalErrorCode = EGlobalErrorCode.Unset,
            Exception innerException = null
        ) : base($"An error occurred while uploading '{remoteFilePath}': '{nativeErrorMessage}' (globalErrorCode={globalErrorCode})", innerException)
        {
            RemoteFilePath = remoteFilePath;
            GlobalErrorCode = globalErrorCode;
        }
    }
}
