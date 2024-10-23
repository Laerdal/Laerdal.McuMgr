using Laerdal.McuMgr.Common.Enums;
using Laerdal.McuMgr.Common.Exceptions;
using Laerdal.McuMgr.FileUploader.Contracts.Enums;

namespace Laerdal.McuMgr.FileUploader.Contracts.Exceptions
{
    public class UploadUnauthorizedException : UploadErroredOutException, IMcuMgrException
    {
        public string RemoteFilePath { get; }
        
        public EMcuMgrErrorCode McuMgrErrorCode { get; }
        public EFileOperationGroupReturnCode GroupReturnCode { get; }

        public UploadUnauthorizedException(string nativeErrorMessage, string remoteFilePath, EMcuMgrErrorCode mcuMgrErrorCode, EFileOperationGroupReturnCode groupReturnCode)
            : base(remoteFilePath, $"{nativeErrorMessage} (McuMgrErrorCode={mcuMgrErrorCode}, GroupReturnCode={groupReturnCode})")
        {
            RemoteFilePath = remoteFilePath;
            McuMgrErrorCode = mcuMgrErrorCode;
            GroupReturnCode = groupReturnCode;
        }
    }
}
