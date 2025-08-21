using Laerdal.McuMgr.Common.Contracts;
using Laerdal.McuMgr.FileDownloading.Contracts.Events;

namespace Laerdal.McuMgr.FileDownloading.Contracts
{
    internal interface IFileDownloaderEventEmittable : ILogEmittable
    {
        void OnCancelled(CancelledEventArgs ea);
        void OnCancelling(CancellingEventArgs ea);

        void OnStateChanged(StateChangedEventArgs ea);
        void OnBusyStateChanged(BusyStateChangedEventArgs ea);
        void OnFatalErrorOccurred(FatalErrorOccurredEventArgs ea);
        void OnFileDownloadStarted(FileDownloadStartedEventArgs ea);
        void OnFileDownloadCompleted(FileDownloadCompletedEventArgs ea);
        void OnFileDownloadProgressPercentageAndDataThroughputChanged(FileDownloadProgressPercentageAndDataThroughputChangedEventArgs ea);
    }
}
