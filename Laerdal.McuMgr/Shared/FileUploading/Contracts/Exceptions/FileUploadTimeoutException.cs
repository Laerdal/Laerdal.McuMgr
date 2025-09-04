// ReSharper disable RedundantExtendsListEntry

using System;

namespace Laerdal.McuMgr.FileUploading.Contracts.Exceptions
{
    public sealed class FileUploadTimeoutException : FileUploadErroredOutException, IUploadException
    {
        public FileUploadTimeoutException(string remoteFilePath, int timeoutInMs, Exception innerException)
            : base(remoteFilePath, $"Failed to upload over to '{remoteFilePath}' on the device within {timeoutInMs}ms", innerException: innerException)
        {
        }
    }
}
