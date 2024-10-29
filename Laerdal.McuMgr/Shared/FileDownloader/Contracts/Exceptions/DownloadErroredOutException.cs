using System;
using Laerdal.McuMgr.Common.Enums;

namespace Laerdal.McuMgr.FileDownloader.Contracts.Exceptions
{
    public class DownloadErroredOutException : Exception, IDownloadException
    {
        public EGlobalErrorCode GlobalErrorCode { get; }

        public DownloadErroredOutException(string errorMessage, EGlobalErrorCode globalErrorCode = EGlobalErrorCode.Unset) : base($"An error occurred while downloading the requested resource: '{errorMessage}'")
        {
            GlobalErrorCode = globalErrorCode;
        }

        public DownloadErroredOutException(string errorMessage, Exception innerException) : base($"An error occurred while downloading the requested resource: '{errorMessage}'", innerException)
        {
        }
    }
}
