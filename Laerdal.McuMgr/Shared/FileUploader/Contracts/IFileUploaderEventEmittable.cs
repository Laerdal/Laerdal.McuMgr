using Laerdal.McuMgr.Common.Contracts;
using Laerdal.McuMgr.FileUploader.Contracts.Events;

namespace Laerdal.McuMgr.FileUploader.Contracts
{
    //must be internal because there is absolutely no point for anyone outside this assembly to be able to raise these events
    internal interface IFileUploaderEventEmittable : ILogEmittable
    {
        void OnCancelled(CancelledEventArgs ea);
        void OnCancelling(CancellingEventArgs ea);
        void OnStateChanged(StateChangedEventArgs ea);
        void OnFileUploaded(FileUploadedEventArgs ea);
        void OnBusyStateChanged(BusyStateChangedEventArgs ea);
        void OnFatalErrorOccurred(FatalErrorOccurredEventArgs ea);
        void OnFileUploadProgressPercentageAndDataThroughputChanged(FileUploadProgressPercentageAndDataThroughputChangedEventArgs ea);
    }
}