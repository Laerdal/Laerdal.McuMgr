using System;
using Laerdal.McuMgr.Common.Enums;
using Laerdal.McuMgr.FileUploader.Contracts.Enums;

namespace Laerdal.McuMgr.FileUploader.Contracts.Exceptions
{
    public class UploadErroredOutException : Exception, IUploadException
    {
        public string RemoteFilePath { get; }
        
        public EMcuMgrErrorCode McuMgrErrorCode { get; } = EMcuMgrErrorCode.Unset;
        public EFileOperationGroupErrorCode FileOperationGroupErrorCode { get; } = EFileOperationGroupErrorCode.Unset;

        protected UploadErroredOutException(string remoteFilePath, string errorMessage, Exception innerException = null)
            : base($"An error occurred while uploading over to '{remoteFilePath}': '{errorMessage}'", innerException)
        {
            RemoteFilePath = remoteFilePath;
        }

        public UploadErroredOutException(
            string nativeErrorMessage,
            string remoteFilePath,
            EMcuMgrErrorCode mcuMgrErrorCode,
            EFileOperationGroupErrorCode fileOperationGroupErrorCode,
            Exception innerException = null
        ) : base($"An error occurred while uploading '{remoteFilePath}': '{nativeErrorMessage}' (mcuMgrErrorCode={mcuMgrErrorCode}, groupReturnCode={fileOperationGroupErrorCode})", innerException)
        {
            RemoteFilePath = remoteFilePath;
            McuMgrErrorCode = mcuMgrErrorCode;
            FileOperationGroupErrorCode = fileOperationGroupErrorCode;
        }
    }
}
