// ReSharper disable UnusedMember.Global
// ReSharper disable EventNeverSubscribedTo.Global

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Laerdal.McuMgr.Common;
using Laerdal.McuMgr.FileDownloader.Contracts.Events;

namespace Laerdal.McuMgr.FileDownloader.Contracts
{
    /// <summary>Downloads a file on a specific Nordic-chip-based BLE device</summary>
    /// <remarks>For the file-downloading process to even commence you need to be authenticated with the AED device that's being targeted.</remarks>
    public interface IFileDownloader : IFileDownloaderEvents, IFileDownloaderCommands
    {
    }

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

    public interface IFileDownloaderCommands
    {
        /// <summary>Holds the last error message emitted</summary>
        public string LastFatalErrorMessage { get; }

        /// <summary>
        /// Begins the file-downloading process on multiple files. Files that cannot be downloaded due to errors will have a null entry in the returned dictionary. To really know when the upgrade process has been completed you have to register to the events emitted by the downloader.
        /// </summary>
        /// <param name="remoteFilePaths">The remote files to download.</param>
        /// <param name="timeoutPerDownloadInMs">The amount of time to wait for each download to complete before skipping it.</param>
        /// <param name="maxRetriesPerDownload">The maximum amount of tries per download before skipping and moving over to the next download.</param>
        /// <param name="sleepTimeBetweenRetriesInMs">The amount of time to sleep between retries.</param>
        /// <returns>A dictionary containing the bytes of each remote file that got fetched over.</returns>
        Task<Dictionary<string, byte[]>> DownloadAsync(
            IEnumerable<string> remoteFilePaths,
            int sleepTimeBetweenRetriesInMs = 1_000,
            int timeoutPerDownloadInMs = -1,
            int maxRetriesPerDownload = 10
        );
        
        /// <summary>
        /// Begins the file-downloading process. To really know when the upgrade process has been completed you have to register to the events emitted by the downloader.
        /// </summary>
        /// <param name="remoteFilePath">The remote file to download.</param>
        /// <param name="timeoutForDownloadInMs">The amount of time to wait for the operation to complete before bailing out.</param>
        /// <param name="maxRetriesCount">The maximum amount of tries before giving up.</param>
        /// <param name="sleepTimeBetweenRetriesInMs">The amount of time to sleep between retries.</param>
        /// <returns>The bytes of the remote file that got fetched over.</returns>
        Task<byte[]> DownloadAsync(
            string remoteFilePath,
            int timeoutForDownloadInMs = -1,
            int maxRetriesCount = 10,
            int sleepTimeBetweenRetriesInMs = 1_000
        );
        
        /// <summary>
        /// Begins the file-downloading process. To really know when the upgrade process has been completed you have to register to the events emitted by the downloader.
        /// </summary>
        /// <param name="remoteFilePath">The remote file to download.</param>
        EFileDownloaderVerdict BeginDownload(string remoteFilePath);

        /// <summary>Cancels the file-downloading process</summary>
        void Cancel();
        
        /// <summary>Disconnects the file-downloader from the targeted device</summary>
        void Disconnect();
    }
}
