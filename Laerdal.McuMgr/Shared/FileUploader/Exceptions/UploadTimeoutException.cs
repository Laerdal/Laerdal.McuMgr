// ReSharper disable RedundantExtendsListEntry

using System;

namespace Laerdal.McuMgr.FileUploader.Exceptions
{
    public sealed class UploadTimeoutException : UploadErroredOutException, IUploadRelatedException
    {
        public UploadTimeoutException(string remoteFolderPath, int timeoutInMs, Exception innerException)
            : base($"Failed to upload over to '{remoteFolderPath}' on the device within {timeoutInMs}ms", innerException)
        {
        }
    }
}