// ReSharper disable RedundantExtendsListEntry

using System;

namespace Laerdal.McuMgr.FileUploader.Contracts.Exceptions
{
    public sealed class UploadTimeoutException : UploadErroredOutException, IUploadException
    {
        public string RemoteFilePath { get; }
        
        public UploadTimeoutException(string remoteFilePath, int timeoutInMs, Exception innerException)
            : base($"Failed to upload over to '{remoteFilePath}' on the device within {timeoutInMs}ms", innerException)
        {
            RemoteFilePath = remoteFilePath;
        }
    }
}
