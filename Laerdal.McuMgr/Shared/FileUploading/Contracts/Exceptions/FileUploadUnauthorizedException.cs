using Laerdal.McuMgr.Common.Enums;

namespace Laerdal.McuMgr.FileUploading.Contracts.Exceptions
{
    public class FileUploadUnauthorizedException : FileUploadErroredOutException
    {
        public FileUploadUnauthorizedException(string nativeErrorMessage, string remoteFilePath, EGlobalErrorCode globalErrorCode)
            : base(remoteFilePath, $"{nativeErrorMessage}", globalErrorCode)
        {
        }
    }
}
