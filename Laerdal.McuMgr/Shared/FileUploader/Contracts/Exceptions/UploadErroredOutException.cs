using System;
using Laerdal.McuMgr.Common.Enums;
using Laerdal.McuMgr.FileUploader.Contracts.Enums;

namespace Laerdal.McuMgr.FileUploader.Contracts.Exceptions
{
    public class UploadErroredOutException : Exception, IUploadException
    {
        public string RemoteFilePath { get; }
        
        public EMcuMgrErrorCode McuMgrErrorCode { get; } = EMcuMgrErrorCode.Unset;
        public EFileUploaderGroupReturnCode GroupReturnCode { get; } = EFileUploaderGroupReturnCode.Unset;

        protected UploadErroredOutException(string remoteFilePath, string errorMessage, Exception innerException = null)
            : base($"An error occurred while uploading over to '{remoteFilePath}': '{errorMessage}'", innerException)
        {
            RemoteFilePath = remoteFilePath;
        }

        public UploadErroredOutException(
            string nativeErrorMessage,
            string remoteFilePath,
            EMcuMgrErrorCode mcuMgrErrorCode,
            EFileUploaderGroupReturnCode groupReturnCode,
            Exception innerException = null
        ) : base($"An error occurred while uploading '{remoteFilePath}': '{nativeErrorMessage}' (mcuMgrErrorCode={mcuMgrErrorCode}, groupReturnCode={groupReturnCode})", innerException)
        {
            RemoteFilePath = remoteFilePath;
            McuMgrErrorCode = mcuMgrErrorCode;
            GroupReturnCode = groupReturnCode;
        }
    }
}
