using Laerdal.McuMgr.Common.Contracts;
using Laerdal.McuMgr.FileUploading.Contracts.Events;

namespace Laerdal.McuMgr.FileUploading.Contracts
{
    //must be internal because there is absolutely no point for anyone outside this assembly to be able to raise these events
    public interface IFileUploaderEventEmittable : ILogEmittable
    {
        void OnCancelled(CancelledEventArgs ea);
        void OnCancelling(CancellingEventArgs ea);
        void OnStateChanged(StateChangedEventArgs ea);
        void OnFileUploadPaused(FileUploadPausedEventArgs ea);
        void OnBusyStateChanged(BusyStateChangedEventArgs ea);
        void OnFileUploadStarted(FileUploadStartedEventArgs ea);
        void OnFileUploadResumed(FileUploadResumedEventArgs ea);
        void OnFatalErrorOccurred(FatalErrorOccurredEventArgs ea);
        void OnFileUploadCompleted(FileUploadCompletedEventArgs ea);
        void OnFileUploadProgressPercentageAndDataThroughputChanged(FileUploadProgressPercentageAndDataThroughputChangedEventArgs ea);
    }
}