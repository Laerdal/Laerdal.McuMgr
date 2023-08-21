﻿using Laerdal.McuMgr.Common;
using Laerdal.McuMgr.FileUploader.Contracts.Events;

namespace Laerdal.McuMgr.FileUploader.Contracts
{
    internal interface IFileUploaderEventEmitters
    {
        void OnCancelled(CancelledEventArgs ea);
        void OnLogEmitted(LogEmittedEventArgs ea);
        void OnStateChanged(StateChangedEventArgs ea);
        void OnUploadCompleted(UploadCompletedEventArgs ea);
        void OnBusyStateChanged(BusyStateChangedEventArgs ea);
        void OnFatalErrorOccurred(FatalErrorOccurredEventArgs ea);
        void OnFileUploadProgressPercentageAndThroughputDataChanged(FileUploadProgressPercentageAndDataThroughputChangedEventArgs ea);
    }
}