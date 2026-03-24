using Laerdal.McuMgr.Common.Enums;

namespace Laerdal.McuMgr.FileUploading.Contracts.Exceptions
{
    // ReSharper disable once RedundantExtendsListEntry
    public sealed class FileUploadErroredOutAbruptlyDisconnectedException : FileUploadErroredOutException, IUploadException
    {
        public FileUploadErroredOutAbruptlyDisconnectedException(string remoteFilePath, EGlobalErrorCode globalErrorCode) : base(
            remoteFilePath: remoteFilePath,
            globalErrorCode: globalErrorCode,
            nativeErrorMessage: $"The device got abruptly disconnected while attempting to upload file '{remoteFilePath}'" //no point to pass the actual native error message because it will be misleading
        )
        {
        }
    }
}