// ReSharper disable RedundantExtendsListEntry

using System;

namespace Laerdal.McuMgr.FileUploader.Contracts.Exceptions
{
    public sealed class UploadTimeoutException : UploadErroredOutException, IUploadException
    {
        public UploadTimeoutException(string remoteFolderPath, int timeoutInMs, Exception innerException)
            : base($"Failed to upload over to '{remoteFolderPath}' on the device within {timeoutInMs}ms", innerException)
        {
        }
    }
}