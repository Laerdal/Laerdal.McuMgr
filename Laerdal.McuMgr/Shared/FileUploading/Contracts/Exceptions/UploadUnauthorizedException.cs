using Laerdal.McuMgr.Common.Enums;

namespace Laerdal.McuMgr.FileUploading.Contracts.Exceptions
{
    public class UploadUnauthorizedException : UploadErroredOutException
    {
        public UploadUnauthorizedException(string nativeErrorMessage, string remoteFilePath, EGlobalErrorCode globalErrorCode)
            : base(remoteFilePath, $"{nativeErrorMessage}", globalErrorCode)
        {
        }
    }
}
