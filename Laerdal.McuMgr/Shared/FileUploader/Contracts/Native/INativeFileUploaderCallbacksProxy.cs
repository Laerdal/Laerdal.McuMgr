using Laerdal.McuMgr.Common.Enums;
using Laerdal.McuMgr.FileUploader.Contracts.Enums;

namespace Laerdal.McuMgr.FileUploader.Contracts.Native
{
    internal interface INativeFileUploaderCallbacksProxy
    {
        public IFileUploaderEventEmittable FileUploader { get; set; }

        void CancelledAdvertisement(string reason = "");
        void CancellingAdvertisement(string reason = "");

        void LogMessageAdvertisement(string message, string category, ELogLevel level, string resource);
        void StateChangedAdvertisement(string resource, EFileUploaderState oldState, EFileUploaderState newState);
        void BusyStateChangedAdvertisement(bool busyNotIdle);
        void FileUploadedAdvertisement(string resource);
        void FatalErrorOccurredAdvertisement(string resource, string errorMessage, EMcuMgrErrorCode mcuMgrErrorCode, EFileOperationGroupErrorCode fileUploaderGroupErrorCode);
        void FileUploadProgressPercentageAndDataThroughputChangedAdvertisement(int progressPercentage, float averageThroughput);
    }
}