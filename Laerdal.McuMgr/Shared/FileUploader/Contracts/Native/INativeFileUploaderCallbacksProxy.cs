using Laerdal.McuMgr.Common.Enums;
using Laerdal.McuMgr.FileUploader.Contracts.Enums;

namespace Laerdal.McuMgr.FileUploader.Contracts.Native
{
    internal interface INativeFileUploaderCallbacksProxy
    {
        public IFileUploaderEventEmittable FileUploader { get; set; }

        void CancelledAdvertisement();
        void LogMessageAdvertisement(string message, string category, ELogLevel level, string resource);
        void StateChangedAdvertisement(string resource, EFileUploaderState oldState, EFileUploaderState newState);
        void BusyStateChangedAdvertisement(bool busyNotIdle);
        void UploadCompletedAdvertisement(string resource);
        void FatalErrorOccurredAdvertisement(string resource, string errorMessage);
        void FileUploadProgressPercentageAndDataThroughputChangedAdvertisement(int progressPercentage, float averageThroughput);
    }
}