// ReSharper disable RedundantExtendsListEntry

using Laerdal.McuMgr.Common.Enums;

namespace Laerdal.McuMgr.FileUploading.Contracts.Exceptions
{
    public sealed class UploadErroredOutRemoteFolderNotFoundException : UploadErroredOutException, IUploadException
    {
        public UploadErroredOutRemoteFolderNotFoundException(
            string nativeErrorMessage,
            string remoteFilePath,
            EGlobalErrorCode globalErrorCode
        ) : base(
            remoteFilePath: remoteFilePath,
            globalErrorCode: globalErrorCode,
            nativeErrorMessage: nativeErrorMessage
        )
        {
        }
    }
}
