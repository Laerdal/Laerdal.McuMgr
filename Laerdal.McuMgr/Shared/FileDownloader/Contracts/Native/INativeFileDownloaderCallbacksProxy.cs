using Laerdal.McuMgr.Common.Enums;
using Laerdal.McuMgr.FileDownloader.Contracts.Enums;

namespace Laerdal.McuMgr.FileDownloader.Contracts.Native
{
    internal interface INativeFileDownloaderCallbacksProxy
    {
        public IFileDownloaderEventEmittable FileDownloader { get; set; }

        void CancelledAdvertisement();
        void LogMessageAdvertisement(string message, string category, ELogLevel level, string resource);
        void StateChangedAdvertisement(string resource, EFileDownloaderState oldState, EFileDownloaderState newState);
        void BusyStateChangedAdvertisement(bool busyNotIdle);
        void DownloadCompletedAdvertisement(string resource, byte[] data);
        void FatalErrorOccurredAdvertisement(string resource, string errorMessage);
        void FileDownloadProgressPercentageAndDataThroughputChangedAdvertisement(int progressPercentage, float averageThroughput);
    }
}