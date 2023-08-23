using Laerdal.McuMgr.Common;
using Laerdal.McuMgr.FileDownloader.Contracts.Events;

namespace Laerdal.McuMgr.FileDownloader.Contracts
{
    internal interface IFileDownloaderEventEmittable
    {
        void OnCancelled(CancelledEventArgs ea);
        void OnLogEmitted(LogEmittedEventArgs ea);
        void OnStateChanged(StateChangedEventArgs ea);
        void OnBusyStateChanged(BusyStateChangedEventArgs ea);
        void OnDownloadCompleted(DownloadCompletedEventArgs ea);
        void OnFatalErrorOccurred(FatalErrorOccurredEventArgs ea);
        void OnFileDownloadProgressPercentageAndThroughputDataChanged(FileDownloadProgressPercentageAndDataThroughputChangedEventArgs ea);        
    }
}