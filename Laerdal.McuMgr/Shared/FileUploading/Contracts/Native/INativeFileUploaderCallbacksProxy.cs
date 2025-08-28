using Laerdal.McuMgr.Common.Enums;
using Laerdal.McuMgr.FileUploading.Contracts.Enums;

namespace Laerdal.McuMgr.FileUploading.Contracts.Native
{
    internal interface INativeFileUploaderCallbacksProxy
    {
        public IFileUploaderEventEmittable FileUploader { get; set; }

        void CancelledAdvertisement(string reason = "");
        void CancellingAdvertisement(string reason = "");

        void LogMessageAdvertisement(string message, string category, ELogLevel level, string resourceId);
        void StateChangedAdvertisement(string resourceId, string remoteFilePath, EFileUploaderState oldState, EFileUploaderState newState, long totalBytesToBeUploaded);
        void BusyStateChangedAdvertisement(bool busyNotIdle);
        void FatalErrorOccurredAdvertisement(string resourceId, string remoteFilePath, string errorMessage, EGlobalErrorCode globalErrorCode);
        void FileUploadProgressPercentageAndDataThroughputChangedAdvertisement(string resourceId, string remoteFilePath, int progressPercentage, float currentThroughputInKBps, float totalAverageThroughputInKBps);
    }
}