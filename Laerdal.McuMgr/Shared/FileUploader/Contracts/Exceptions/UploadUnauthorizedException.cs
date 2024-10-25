using Laerdal.McuMgr.Common.Enums;
using Laerdal.McuMgr.Common.Exceptions;
using Laerdal.McuMgr.FileUploader.Contracts.Enums;

namespace Laerdal.McuMgr.FileUploader.Contracts.Exceptions
{
    public class UploadUnauthorizedException : UploadErroredOutException, IMcuMgrException
    {
        public string RemoteFilePath { get; }
        
        public EMcuMgrErrorCode McuMgrErrorCode { get; }
        public EFileOperationGroupErrorCode FileOperationGroupErrorCode { get; }

        public UploadUnauthorizedException(string nativeErrorMessage, string remoteFilePath, EMcuMgrErrorCode mcuMgrErrorCode, EFileOperationGroupErrorCode fileOperationGroupErrorCode)
            : base(remoteFilePath, $"{nativeErrorMessage} (McuMgrErrorCode={mcuMgrErrorCode}, GroupReturnCode={fileOperationGroupErrorCode})")
        {
            RemoteFilePath = remoteFilePath;
            McuMgrErrorCode = mcuMgrErrorCode;
            FileOperationGroupErrorCode = fileOperationGroupErrorCode;
        }
    }
}
