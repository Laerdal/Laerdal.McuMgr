// ReSharper disable RedundantExtendsListEntry

using System;

namespace Laerdal.McuMgr.FileUploading.Contracts.Exceptions
{
    public class FileUploadInternalErrorException : FileUploadErroredOutException, IUploadException
    {
        public FileUploadInternalErrorException(string remoteFilePath, string message = "(no details)", Exception innerException = null)
            : base(remoteFilePath, $"An internal error occured - report what you did to reproduce this because this is most probably a bug: {message}", innerException: innerException)
        {
        }
    }
}
