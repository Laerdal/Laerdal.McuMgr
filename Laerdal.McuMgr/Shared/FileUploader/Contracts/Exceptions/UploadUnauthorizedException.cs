using Laerdal.McuMgr.Common.Enums;
using Laerdal.McuMgr.Common.Exceptions;

namespace Laerdal.McuMgr.FileUploader.Contracts.Exceptions
{
    public class UploadUnauthorizedException : UploadErroredOutException, IMcuMgrException
    {
        public string RemoteFilePath { get; }

        public UploadUnauthorizedException(string nativeErrorMessage, string remoteFilePath, EGlobalErrorCode globalErrorCode)
            : base(remoteFilePath, $"{nativeErrorMessage}", globalErrorCode)
        {
            RemoteFilePath = remoteFilePath;
        }
    }
}
