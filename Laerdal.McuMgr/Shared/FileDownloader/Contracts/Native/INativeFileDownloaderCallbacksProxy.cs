using Laerdal.McuMgr.Common;
using Laerdal.McuMgr.FileDownloader.Contracts.Enums;

namespace Laerdal.McuMgr.FileDownloader.Contracts.Native
{
    internal interface INativeFileDownloaderCallbacksProxy
    {
        public IFileDownloaderEventEmitters FileDownloader { get; set; }

        void CancelledAdvertisement();
        void LogMessageAdvertisement(string message, string category, ELogLevel level, string resource);
        void StateChangedAdvertisement(string resource, EFileDownloaderState oldState, EFileDownloaderState newState);
        void BusyStateChangedAdvertisement(bool busyNotIdle);
        void DownloadCompletedAdvertisement(string resource, byte[] data);
        void FatalErrorOccurredAdvertisement(string resource, string errorMessage);
        void FileDownloadProgressPercentageAndThroughputDataChangedAdvertisement(int progressPercentage, float averageThroughput);
    }
}