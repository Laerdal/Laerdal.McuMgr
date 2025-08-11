using Laerdal.McuMgr.Common.Enums;
using Laerdal.McuMgr.FileDownloading.Contracts.Enums;

namespace Laerdal.McuMgr.FileDownloading.Contracts.Native
{
    internal interface INativeFileDownloaderCallbacksProxy
    {
        public IFileDownloaderEventEmittable FileDownloader { get; set; }

        void CancelledAdvertisement();
        void LogMessageAdvertisement(string message, string category, ELogLevel level, string resource);
        void StateChangedAdvertisement(string resourceId, EFileDownloaderState oldState, EFileDownloaderState newState);
        void BusyStateChangedAdvertisement(bool busyNotIdle);
        void FatalErrorOccurredAdvertisement(string resourceId, string errorMessage, EGlobalErrorCode globalErrorCode);
        void FileDownloadStartedAdvertisement(string resourceId);
        void FileDownloadCompletedAdvertisement(string resourceId, byte[] data);
        void FileDownloadProgressPercentageAndDataThroughputChangedAdvertisement(string resourceId, int progressPercentage, float currentThroughputInKbps, float totalAverageThroughputInKbps);
    }
}