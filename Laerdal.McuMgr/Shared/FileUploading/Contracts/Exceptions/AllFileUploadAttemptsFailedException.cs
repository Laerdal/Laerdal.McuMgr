using System;

namespace Laerdal.McuMgr.FileUploading.Contracts.Exceptions
{
    // ReSharper disable once RedundantExtendsListEntry
    public class AllFileUploadAttemptsFailedException : FileUploadErroredOutException, IUploadException
    {
        public AllFileUploadAttemptsFailedException(string remoteFilePath, int triesCount, Exception innerException = null) : base(
            innerException: innerException,
            remoteFilePath: remoteFilePath,
            nativeErrorMessage: $"Failed to upload '{remoteFilePath}' after trying {triesCount} time(s)"
        )
        {
        }
    }
}