using System;
using Laerdal.McuMgr.Common;
using Laerdal.McuMgr.FileUploader.Contracts.Events;

namespace Laerdal.McuMgr.FileUploader.Contracts
{
    public interface IFileUploaderEventSubscribable
    {
        /// <summary>Event raised when a fatal error occurs</summary>
        event EventHandler<FatalErrorOccurredEventArgs> FatalErrorOccurred;

        /// <summary>Event raised when the operation gets cancelled</summary>
        event EventHandler<CancelledEventArgs> Cancelled;

        /// <summary>Event raised when a log gets emitted</summary>
        event EventHandler<LogEmittedEventArgs> LogEmitted;

        /// <summary>Event raised when the file-uploading state changes</summary>
        event EventHandler<StateChangedEventArgs> StateChanged;

        /// <summary>Event raised when the file-uploading completes successfully</summary>
        event EventHandler<UploadCompletedEventArgs> UploadCompleted;

        /// <summary>Event raised when the file-uploading busy-state changes which happens when data start or stop being transmitted</summary>
        event EventHandler<BusyStateChangedEventArgs> BusyStateChanged;
        
        /// <summary>Event raised when the file-uploading process progresses in terms of uploading the firmware files across</summary>
        event EventHandler<FileUploadProgressPercentageAndDataThroughputChangedEventArgs> FileUploadProgressPercentageAndDataThroughputChanged;
    }
}