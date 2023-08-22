using System;
using Laerdal.McuMgr.Common;
using Laerdal.McuMgr.FileDownloader.Contracts.Events;

namespace Laerdal.McuMgr.FileDownloader.Contracts
{
    public interface IFileDownloaderEvents
    {
        /// <summary>Event raised when an error occurs</summary>
        public event EventHandler<FatalErrorOccurredEventArgs> FatalErrorOccurred;

        /// <summary>Event raised when the operation gets cancelled</summary>
        public event EventHandler<CancelledEventArgs> Cancelled;

        /// <summary>Event raised when a log gets emitted</summary>
        public event EventHandler<LogEmittedEventArgs> LogEmitted;

        /// <summary>Event raised when the state changes</summary>
        public event EventHandler<StateChangedEventArgs> StateChanged;

        /// <summary>Event raised when the firmware-installation busy-state changes which happens when data start or stop being transmitted</summary>
        public event EventHandler<BusyStateChangedEventArgs> BusyStateChanged;

        /// <summary>Event raised when the download is complete</summary>
        public event EventHandler<DownloadCompletedEventArgs> DownloadCompleted;
        
        /// <summary>Event raised when the firmware-installation process progresses in terms of downloading the firmware files across</summary>
        public event EventHandler<FileDownloadProgressPercentageAndDataThroughputChangedEventArgs> FileDownloadProgressPercentageAndDataThroughputChanged;
    }
}