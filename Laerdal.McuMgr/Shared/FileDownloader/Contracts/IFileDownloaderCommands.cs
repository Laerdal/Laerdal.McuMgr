using System.Collections.Generic;
using System.Threading.Tasks;
using Laerdal.McuMgr.FileDownloader.Contracts.Enums;

namespace Laerdal.McuMgr.FileDownloader.Contracts
{
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
        Task<IDictionary<string, byte[]>> DownloadAsync(
            IEnumerable<string> remoteFilePaths,
            int timeoutPerDownloadInMs = -1,
            int maxRetriesPerDownload = 10,
            int sleepTimeBetweenRetriesInMs = 0
        );

        /// <summary>
        /// Begins the file-downloading process. To really know when the upgrade process has been completed you have to register to the events emitted by the downloader.
        /// </summary>
        /// <param name="remoteFilePath">The remote file to download.</param>
        /// <param name="timeoutForDownloadInMs">The amount of time to wait for the operation to complete before bailing out.</param>
        /// <param name="maxRetriesCount">The maximum amount of tries before giving up.</param>
        /// <param name="sleepTimeBetweenRetriesInMs">The amount of time to sleep between retries.</param>
        /// <param name="gracefulCancellationTimeoutInMs">The time to wait (in milliseconds) for a cancellation request to be properly handled. If this timeout expires then the mechanism will bail out forcefully without waiting for the underlying native code to cleanup properly.</param>
        /// <returns>The bytes of the remote file that got fetched over.</returns>
        Task<byte[]> DownloadAsync(
            string remoteFilePath,
            int timeoutForDownloadInMs = -1,
            int maxRetriesCount = 10,
            int sleepTimeBetweenRetriesInMs = 1_000,
            int gracefulCancellationTimeoutInMs = 2_500
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