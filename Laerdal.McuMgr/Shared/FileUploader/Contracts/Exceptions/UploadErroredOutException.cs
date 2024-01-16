using System;

namespace Laerdal.McuMgr.FileUploader.Contracts.Exceptions
{
    public class UploadErroredOutException : Exception, IUploadException
    {
        public string RemoteFilePath { get; }

        public UploadErroredOutException(string remoteFilePath, string errorMessage, Exception innerException = null)
            : base($"An error occurred while uploading '{remoteFilePath}' the requested resource: '{errorMessage}'", innerException)
        {
            RemoteFilePath = remoteFilePath;
        }
    }
}