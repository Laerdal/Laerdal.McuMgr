// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable ClassNeverInstantiated.Global

using Laerdal.McuMgr.Common.Enums;
using Laerdal.McuMgr.Common.Events;
using Laerdal.McuMgr.FileUploader.Contracts.Enums;

namespace Laerdal.McuMgr.FileDownloader.Contracts.Events
{
    public readonly struct FatalErrorOccurredEventArgs : IMcuMgrEventArgs
    {
        public string Resource { get; }
        public string ErrorMessage { get; }
        public EMcuMgrErrorCode McuMgrErrorCode { get; }
        public EFileOperationGroupErrorCode FileOperationGroupErrorCode { get; }

        public FatalErrorOccurredEventArgs(string resource, string errorMessage, EMcuMgrErrorCode mcuMgrErrorCode, EFileOperationGroupErrorCode fileOperationGroupErrorCode)
        {
            Resource = resource;
            ErrorMessage = errorMessage;
            McuMgrErrorCode = mcuMgrErrorCode;
            FileOperationGroupErrorCode = fileOperationGroupErrorCode;
        }
    }
}
