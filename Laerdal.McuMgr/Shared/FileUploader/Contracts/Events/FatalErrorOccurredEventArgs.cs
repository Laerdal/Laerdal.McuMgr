// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable ClassNeverInstantiated.Global

using Laerdal.McuMgr.Common.Enums;
using Laerdal.McuMgr.Common.Events;
using Laerdal.McuMgr.FileUploader.Contracts.Enums;

namespace Laerdal.McuMgr.FileUploader.Contracts.Events
{
    public readonly struct FatalErrorOccurredEventArgs : IMcuMgrEventArgs
    {
        public string ErrorMessage { get; }
        public string RemoteFilePath { get; }

        public EMcuMgrErrorCode McuMgrErrorCode { get; }
        public EFileUploaderGroupReturnCode FileUploaderGroupReturnCode { get; }

        public FatalErrorOccurredEventArgs(string remoteFilePath, string errorMessage, EMcuMgrErrorCode mcuMgrErrorCode, EFileUploaderGroupReturnCode fileUploaderGroupReturnCode)
        {
            ErrorMessage = errorMessage;
            RemoteFilePath = remoteFilePath;
            McuMgrErrorCode = mcuMgrErrorCode;
            FileUploaderGroupReturnCode = fileUploaderGroupReturnCode;
        }
    }
}
