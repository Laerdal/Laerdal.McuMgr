using System;
using Laerdal.McuMgr.Common.Contracts;
using Laerdal.McuMgr.Common.Enums;
using Laerdal.McuMgr.Common.Events;
using Laerdal.McuMgr.Common.Extensions;
using Laerdal.McuMgr.Common.Helpers;
using Laerdal.McuMgr.FileDownloading.Contracts;
using Laerdal.McuMgr.FileDownloading.Contracts.Events;

namespace Laerdal.McuMgr.FileDownloading
{
    public partial class FileDownloader
    {
        private event ZeroCopyEventHelpers.ZeroCopyEventHandler<LogEmittedEventArgs> _logEmitted;

        private event EventHandler<CancelledEventArgs> _cancelled;
        private event EventHandler<CancellingEventArgs> _cancelling;
        private event EventHandler<StateChangedEventArgs> _stateChanged;
        private event EventHandler<BusyStateChangedEventArgs> _busyStateChanged;
        private event EventHandler<FatalErrorOccurredEventArgs> _fatalErrorOccurred;
        private event EventHandler<FileDownloadPausedEventArgs> _fileDownloadPaused;
        private event EventHandler<FileDownloadResumedEventArgs> _fileDownloadResumed;
        private event EventHandler<FileDownloadStartedEventArgs> _fileDownloadStarted;
        private event EventHandler<FileDownloadCompletedEventArgs> _fileDownloadCompleted;
        private event EventHandler<FileDownloadProgressPercentageAndDataThroughputChangedEventArgs> _fileDownloadProgressPercentageAndDataThroughputChanged;

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

        public event EventHandler<CancelledEventArgs> Cancelled
        {
            add
            {
                _cancelled -= value;
                _cancelled += value;
            }
            remove => _cancelled -= value;
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

        public event EventHandler<FileDownloadStartedEventArgs> FileDownloadStarted
        {
            add
            {
                _fileDownloadStarted -= value;
                _fileDownloadStarted += value;
            }
            remove => _fileDownloadStarted -= value;
        }

        public event EventHandler<FileDownloadPausedEventArgs> FileDownloadPaused
        {
            add
            {
                _fileDownloadPaused -= value;
                _fileDownloadPaused += value;
            }
            remove => _fileDownloadPaused -= value;
        }

        public event EventHandler<FileDownloadResumedEventArgs> FileDownloadResumed
        {
            add
            {
                _fileDownloadResumed -= value;
                _fileDownloadResumed += value;
            }
            remove => _fileDownloadResumed -= value;
        }

        public event EventHandler<FileDownloadCompletedEventArgs> FileDownloadCompleted
        {
            add
            {
                _fileDownloadCompleted -= value;
                _fileDownloadCompleted += value;
            }
            remove => _fileDownloadCompleted -= value;
        }

        public event EventHandler<FileDownloadProgressPercentageAndDataThroughputChangedEventArgs> FileDownloadProgressPercentageAndDataThroughputChanged
        {
            add
            {
                _fileDownloadProgressPercentageAndDataThroughputChanged -= value;
                _fileDownloadProgressPercentageAndDataThroughputChanged += value;
            }
            remove => _fileDownloadProgressPercentageAndDataThroughputChanged -= value;
        }


        void ILogEmittable.OnLogEmitted(in LogEmittedEventArgs ea) => OnLogEmitted(in ea);
        void IFileDownloaderEventEmittable.OnCancelled(CancelledEventArgs ea) => OnCancelled(ea); //just to make the class unit-test friendly without making the methods public
        void IFileDownloaderEventEmittable.OnCancelling(CancellingEventArgs ea) => OnCancelling(ea); //just to make the class unit-test friendly without making the methods public
        void IFileDownloaderEventEmittable.OnStateChanged(StateChangedEventArgs ea) => OnStateChanged(ea);
        void IFileDownloaderEventEmittable.OnBusyStateChanged(BusyStateChangedEventArgs ea) => OnBusyStateChanged(ea);
        void IFileDownloaderEventEmittable.OnFileDownloadPaused(FileDownloadPausedEventArgs ea) => OnFileDownloadPaused(ea);
        void IFileDownloaderEventEmittable.OnFatalErrorOccurred(FatalErrorOccurredEventArgs ea) => OnFatalErrorOccurred(ea);
        void IFileDownloaderEventEmittable.OnFileDownloadResumed(FileDownloadResumedEventArgs ea) => OnFileDownloadResumed(ea);
        void IFileDownloaderEventEmittable.OnFileDownloadStarted(FileDownloadStartedEventArgs ea) => OnFileDownloadStarted(ea);
        void IFileDownloaderEventEmittable.OnFileDownloadCompleted(FileDownloadCompletedEventArgs ea) => OnFileDownloadCompleted(ea);
        void IFileDownloaderEventEmittable.OnFileDownloadProgressPercentageAndDataThroughputChanged(FileDownloadProgressPercentageAndDataThroughputChangedEventArgs ea) => OnFileDownloadProgressPercentageAndDataThroughputChanged(ea);

        private void OnCancelled(CancelledEventArgs ea) => _cancelled?.InvokeAllEventHandlersAndIgnoreExceptions(this, ea);
        private void OnCancelling(CancellingEventArgs ea) => _cancelling?.InvokeAllEventHandlersAndIgnoreExceptions(this, ea);
        private void OnLogEmitted(in LogEmittedEventArgs ea) => _logEmitted?.InvokeAndIgnoreExceptions(this, ea); // in the special case of log-emitted we prefer the .invoke() flavour for the sake of performance
        private void OnStateChanged(StateChangedEventArgs ea) => _stateChanged?.InvokeAllEventHandlersAndIgnoreExceptions(this, ea);
        private void OnBusyStateChanged(BusyStateChangedEventArgs ea) => _busyStateChanged?.InvokeAllEventHandlersAndIgnoreExceptions(this, ea);
        private void OnFileDownloadProgressPercentageAndDataThroughputChanged(FileDownloadProgressPercentageAndDataThroughputChangedEventArgs ea) => _fileDownloadProgressPercentageAndDataThroughputChanged?.InvokeAndIgnoreExceptions(this, ea);

        private void OnFileDownloadCompleted(FileDownloadCompletedEventArgs ea)
        {
            OnLogEmitted(new(level: ELogLevel.Info, message: "[FD.OFUC.010] Downloading complete", category: "FileDownloader", resource: ea.RemoteFilePath));
            
            _fileDownloadCompleted?.InvokeAllEventHandlersAndIgnoreExceptions(this, ea);
        }
        
        private void OnFileDownloadPaused(FileDownloadPausedEventArgs ea)
        {
            OnLogEmitted(new(level: ELogLevel.Info, message: "[FD.OFUP.010] Downloading paused", category: "FileDownloader", resource: ea.RemoteFilePath));
            
            _fileDownloadPaused?.InvokeAllEventHandlersAndIgnoreExceptions(this, ea);
        }
        
        private void OnFileDownloadStarted(FileDownloadStartedEventArgs ea)
        {
            OnLogEmitted(new(level: ELogLevel.Info, message: $"[FD.OFUS.010] Starting Downloading of '{ea.TotalBytesToBeDownloaded}' bytes", category: "FileDownloader", resource: ea.RemoteFilePath));
            
            _fileDownloadStarted?.InvokeAllEventHandlersAndIgnoreExceptions(this, ea);
        }
        
        private void OnFileDownloadResumed(FileDownloadResumedEventArgs ea)
        {
            OnLogEmitted(new(level: ELogLevel.Info, message: "[FD.OFUR.010] Resumed Downloading of asset", category: "FileDownloader", resource: ea.RemoteFilePath));
            
            _fileDownloadResumed?.InvokeAllEventHandlersAndIgnoreExceptions(this, ea);
        }
        
        private void OnFatalErrorOccurred(FatalErrorOccurredEventArgs ea)
        {
            OnLogEmitted(new LogEmittedEventArgs(level: ELogLevel.Error, message: $"[{nameof(ea.GlobalErrorCode)}='{ea.GlobalErrorCode}'] {ea.ErrorMessage}", resource: ea.Resource, category: "file-downloader"));

            _fatalErrorOccurred?.InvokeAllEventHandlersAndIgnoreExceptions(this, ea);
        }
    }
}