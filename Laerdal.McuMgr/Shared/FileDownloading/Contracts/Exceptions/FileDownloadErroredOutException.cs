using System;
using Laerdal.McuMgr.Common.Enums;

namespace Laerdal.McuMgr.FileDownloading.Contracts.Exceptions
{
    public class FileDownloadErroredOutException : Exception, IDownloadException
    {
        public EGlobalErrorCode GlobalErrorCode { get; }

        public FileDownloadErroredOutException(string errorMessage, EGlobalErrorCode globalErrorCode = EGlobalErrorCode.Unset) : base($"An error occurred while downloading the requested resource: '{errorMessage}'")
        {
            GlobalErrorCode = globalErrorCode;
        }

        public FileDownloadErroredOutException(string errorMessage, Exception innerException) : base($"An error occurred while downloading the requested resource: '{errorMessage}'", innerException)
        {
        }
    }
}
