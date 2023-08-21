using Laerdal.McuMgr.Common;

namespace Laerdal.McuMgr.FileUploader.Contracts
{
    internal interface INativeFileUploaderCallbacksProxy
    {
        public IFileUploaderEventEmitters FileUploader { get; set; }

        void CancelledAdvertisement();
        void LogMessageAdvertisement(string message, string category, ELogLevel level, string resource);
        void StateChangedAdvertisement(string resource, EFileUploaderState oldState, EFileUploaderState newState);
        void BusyStateChangedAdvertisement(bool busyNotIdle);
        void UploadCompletedAdvertisement(string resource);
        void FatalErrorOccurredAdvertisement(string resource, string errorMessage);
        void FileUploadProgressPercentageAndThroughputDataChangedAdvertisement(int progressPercentage, float averageThroughput);
    }
}