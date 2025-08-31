using System;
using Laerdal.McuMgr.Common.Contracts;
using Laerdal.McuMgr.Common.Enums;
using Laerdal.McuMgr.Common.Events;
using Laerdal.McuMgr.Common.Extensions;
using Laerdal.McuMgr.Common.Helpers;
using Laerdal.McuMgr.FileUploading.Contracts;
using Laerdal.McuMgr.FileUploading.Contracts.Events;

namespace Laerdal.McuMgr.FileUploading
{
    public partial class FileUploader
    {
        private event ZeroCopyEventHelpers.ZeroCopyEventHandler<LogEmittedEventArgs> _logEmitted;
        
        private event EventHandler<CancelledEventArgs> _cancelled;
        private event EventHandler<CancellingEventArgs> _cancelling;
        private event EventHandler<StateChangedEventArgs> _stateChanged;
        private event EventHandler<BusyStateChangedEventArgs> _busyStateChanged;
        private event EventHandler<FileUploadPausedEventArgs> _fileUploadPaused;
        private event EventHandler<FileUploadStartedEventArgs> _fileUploadStarted;
        private event EventHandler<FileUploadResumedEventArgs> _fileUploadResumed;
        private event EventHandler<FatalErrorOccurredEventArgs> _fatalErrorOccurred;
        private event EventHandler<FileUploadCompletedEventArgs> _fileUploadCompleted;
        private event EventHandler<FileUploadProgressPercentageAndDataThroughputChangedEventArgs> _fileUploadProgressPercentageAndDataThroughputChanged;

        public event EventHandler<FatalErrorOccurredEventArgs> FatalErrorOccurred
        {
            add
            {
                _fatalErrorOccurred -= value;
                _fatalErrorOccurred += value;
            }
            remove => _fatalErrorOccurred -= value;
        }

        public event ZeroCopyEventHelpers.ZeroCopyEventHandler<LogEmittedEventArgs> LogEmitted
        {
            add
            {
                _logEmitted -= value;
                _logEmitted += value;
            }
            remove => _logEmitted -= value;
        }

        public event EventHandler<CancellingEventArgs> Cancelling
        {
            add
            {
                _cancelling -= value;
                _cancelling += value;
            }
            remove => _cancelling -= value;
        }

        public event EventHandler<CancelledEventArgs> Cancelled
        {
            add
            {
                _cancelled -= value;
                _cancelled += value;
            }
            remove => _cancelled -= value;
        }

        public event EventHandler<BusyStateChangedEventArgs> BusyStateChanged
        {
            add
            {
                _busyStateChanged -= value;
                _busyStateChanged += value;
            }
            remove => _busyStateChanged -= value;
        }
        
        public event EventHandler<StateChangedEventArgs> StateChanged
        {
            add
            {
                _stateChanged -= value;
                _stateChanged += value;
            }
            remove => _stateChanged -= value;
        }
        
        public event EventHandler<FileUploadStartedEventArgs> FileUploadStarted
        {
            add
            {
                _fileUploadStarted -= value;
                _fileUploadStarted += value;
            }
            remove => _fileUploadStarted -= value;
        }

        public event EventHandler<FileUploadPausedEventArgs> FileUploadPaused
        {
            add
            {
                _fileUploadPaused -= value;
                _fileUploadPaused += value;
            }
            remove => _fileUploadPaused -= value;
        }
        
        public event EventHandler<FileUploadResumedEventArgs> FileUploadResumed
        {
            add
            {
                _fileUploadResumed -= value;
                _fileUploadResumed += value;
            }
            remove => _fileUploadResumed -= value;
        }
        
        public event EventHandler<FileUploadCompletedEventArgs> FileUploadCompleted
        {
            add
            {
                _fileUploadCompleted -= value;
                _fileUploadCompleted += value;
            }
            remove => _fileUploadCompleted -= value;
        }

        public event EventHandler<FileUploadProgressPercentageAndDataThroughputChangedEventArgs> FileUploadProgressPercentageAndDataThroughputChanged
        {
            add
            {
                _fileUploadProgressPercentageAndDataThroughputChanged -= value;
                _fileUploadProgressPercentageAndDataThroughputChanged += value;
            }
            remove => _fileUploadProgressPercentageAndDataThroughputChanged -= value;
        }

        void ILogEmittable.OnLogEmitted(in LogEmittedEventArgs ea) => OnLogEmitted(in ea);
        void IFileUploaderEventEmittable.OnCancelled(CancelledEventArgs ea) => OnCancelled(ea);
        void IFileUploaderEventEmittable.OnCancelling(CancellingEventArgs ea) => OnCancelling(ea);
        void IFileUploaderEventEmittable.OnStateChanged(StateChangedEventArgs ea) => OnStateChanged(ea);
        void IFileUploaderEventEmittable.OnBusyStateChanged(BusyStateChangedEventArgs ea) => OnBusyStateChanged(ea);
        void IFileUploaderEventEmittable.OnFileUploadPaused(FileUploadPausedEventArgs ea) => OnFileUploadPaused(ea);
        void IFileUploaderEventEmittable.OnFileUploadStarted(FileUploadStartedEventArgs ea) => OnFileUploadStarted(ea);
        void IFileUploaderEventEmittable.OnFileUploadResumed(FileUploadResumedEventArgs ea) => OnFileUploadResumed(ea);
        void IFileUploaderEventEmittable.OnFatalErrorOccurred(FatalErrorOccurredEventArgs ea) => OnFatalErrorOccurred(ea);
        void IFileUploaderEventEmittable.OnFileUploadCompleted(FileUploadCompletedEventArgs ea) => OnFileUploadCompleted(ea);
        void IFileUploaderEventEmittable.OnFileUploadProgressPercentageAndDataThroughputChanged(FileUploadProgressPercentageAndDataThroughputChangedEventArgs ea) => OnFileUploadProgressPercentageAndDataThroughputChanged(ea);

        private void OnCancelled(CancelledEventArgs ea) => _cancelled?.InvokeAllEventHandlersAndIgnoreExceptions(this, ea);
        private void OnCancelling(CancellingEventArgs ea) => _cancelling?.InvokeAllEventHandlersAndIgnoreExceptions(this, ea);
        private void OnLogEmitted(in LogEmittedEventArgs ea) => _logEmitted?.InvokeAndIgnoreExceptions(this, ea); // in the special case of log-emitted we prefer the .invoke() flavour for the sake of performance
        private void OnStateChanged(StateChangedEventArgs ea) => _stateChanged?.InvokeAllEventHandlersAndIgnoreExceptions(this, ea);

        private void OnBusyStateChanged(BusyStateChangedEventArgs ea) => _busyStateChanged?.InvokeAllEventHandlersAndIgnoreExceptions(this, ea);
        private void OnFileUploadProgressPercentageAndDataThroughputChanged(FileUploadProgressPercentageAndDataThroughputChangedEventArgs ea) => _fileUploadProgressPercentageAndDataThroughputChanged?.InvokeAndIgnoreExceptions(this, ea);

        private void OnFileUploadCompleted(FileUploadCompletedEventArgs ea)
        {
            OnLogEmitted(new(level: ELogLevel.Trace, message: "[FU.OFUC.010] Uploading complete", category: "FileUploader", resource: ea.ResourceId));
            
            _fileUploadCompleted?.InvokeAllEventHandlersAndIgnoreExceptions(this, ea);
        }

        private void OnFileUploadPaused(FileUploadPausedEventArgs ea)
        {
            OnLogEmitted(new(level: ELogLevel.Info, message: "[FU.OFUP.010] Uploading paused", category: "FileUploader", resource: ea.ResourceId));

            _fileUploadPaused?.InvokeAllEventHandlersAndIgnoreExceptions(this, ea);
        }
        
        private void OnFileUploadStarted(FileUploadStartedEventArgs ea)
        {
            OnLogEmitted(new(level: ELogLevel.Info, message: $"[FU.OFUS.010] Starting uploading of '{ea.TotalBytesToBeUploaded}' bytes", category: "FileUploader", resource: ea.ResourceId));

            _fileUploadStarted?.InvokeAllEventHandlersAndIgnoreExceptions(this, ea);
        }
        
        private void OnFileUploadResumed(FileUploadResumedEventArgs ea)
        {
            OnLogEmitted(new(level: ELogLevel.Info, message: "[FU.OFUR.010] Resumed uploading of asset", category: "FileUploader", resource: ea.ResourceId));

            _fileUploadResumed?.InvokeAllEventHandlersAndIgnoreExceptions(this, ea);
        }
        
        private void OnFatalErrorOccurred(FatalErrorOccurredEventArgs ea)
        {
            OnLogEmitted(new LogEmittedEventArgs(level: ELogLevel.Error, message: $"[{nameof(ea.GlobalErrorCode)}='{ea.GlobalErrorCode}'] {ea.ErrorMessage}", resource: ea.RemoteFilePath, category: "file-uploader"));
            
            _fatalErrorOccurred?.InvokeAllEventHandlersAndIgnoreExceptions(this, ea);
        }
    }
}
