using Laerdal.McuMgr.Common.Contracts;
using Laerdal.McuMgr.FileDownloading.Contracts.Events;

namespace Laerdal.McuMgr.FileDownloading.Contracts
{
    internal interface IFileDownloaderEventEmittable : ILogEmittable
    {
        void OnCancelled(CancelledEventArgs ea);
        void OnStateChanged(StateChangedEventArgs ea);
        void OnBusyStateChanged(BusyStateChangedEventArgs ea);
        void OnDownloadCompleted(DownloadCompletedEventArgs ea);
        void OnFatalErrorOccurred(FatalErrorOccurredEventArgs ea);
        void OnFileDownloadProgressPercentageAndDataThroughputChanged(FileDownloadProgressPercentageAndDataThroughputChangedEventArgs ea);        
    }
}