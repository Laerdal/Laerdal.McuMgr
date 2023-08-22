using System;
using Laerdal.McuMgr.Common;
using Laerdal.McuMgr.FileUploader.Contracts.Events;

namespace Laerdal.McuMgr.FileUploader.Contracts
{
    public interface IFileUploaderEvents
    {
        /// <summary>Event raised when an error occurs</summary>
        public event EventHandler<FatalErrorOccurredEventArgs> FatalErrorOccurred;

        /// <summary>Event raised when the operation gets cancelled</summary>
        public event EventHandler<CancelledEventArgs> Cancelled;

        /// <summary>Event raised when a log gets emitted</summary>
        public event EventHandler<LogEmittedEventArgs> LogEmitted;

        /// <summary>Event raised when the file-uploading state changes</summary>
        public event EventHandler<StateChangedEventArgs> StateChanged;

        /// <summary>Event raised when the file-uploading completes successfully</summary>
        public event EventHandler<UploadCompletedEventArgs> UploadCompleted;

        /// <summary>Event raised when the file-uploading busy-state changes which happens when data start or stop being transmitted</summary>
        public event EventHandler<BusyStateChangedEventArgs> BusyStateChanged;
    }
}