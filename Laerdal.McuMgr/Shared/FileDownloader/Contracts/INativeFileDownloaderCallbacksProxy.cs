using Laerdal.McuMgr.Common;

namespace Laerdal.McuMgr.FileDownloader.Contracts
{
    internal interface INativeFileDownloaderCallbacksProxy
    {
        public IFileDownloaderEventEmitters FileDownloader { get; set; }

        void CancelledAdvertisement();
        void LogMessageAdvertisement(string message, string category, ELogLevel level, string resource);
        void StateChangedAdvertisement(string resource, EFileDownloaderState oldState, EFileDownloaderState newState);
        void BusyStateChangedAdvertisement(bool busyNotIdle);
        void DownloadCompletedAdvertisement(string resource, byte[] data);
        void FatalErrorOccurredAdvertisement(string errorMessage);
        void FileDownloadProgressPercentageAndThroughputDataChangedAdvertisement(int progressPercentage, float averageThroughput);
    }
}