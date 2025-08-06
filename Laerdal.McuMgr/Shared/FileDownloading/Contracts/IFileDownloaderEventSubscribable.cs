using System;
using Laerdal.McuMgr.Common.Events;
using Laerdal.McuMgr.Common.Helpers;
using Laerdal.McuMgr.FileDownloading.Contracts.Events;

namespace Laerdal.McuMgr.FileDownloading.Contracts
{
    public interface IFileDownloaderEventSubscribable
    {
        /// <summary>Event raised when a fatal error occurs</summary>
        event EventHandler<FatalErrorOccurredEventArgs> FatalErrorOccurred;

        /// <summary>Event raised when the operation gets cancelled</summary>
        event EventHandler<CancelledEventArgs> Cancelled;

        /// <summary>Event raised when a log gets emitted</summary>
        event ZeroCopyEventHelpers.ZeroCopyEventHandler<LogEmittedEventArgs> LogEmitted;

        /// <summary>Event raised when the state changes</summary>
        event EventHandler<StateChangedEventArgs> StateChanged;

        /// <summary>Event raised when the firmware-installation busy-state changes which happens when data start or stop being transmitted</summary>
        event EventHandler<BusyStateChangedEventArgs> BusyStateChanged;

        /// <summary>Event raised when the download is complete</summary>
        event EventHandler<DownloadCompletedEventArgs> DownloadCompleted;

        /// <summary>Event raised when the firmware-installation process progresses in terms of downloading the firmware files across</summary>
        event EventHandler<FileDownloadProgressPercentageAndDataThroughputChangedEventArgs> FileDownloadProgressPercentageAndDataThroughputChanged;
    }
}
