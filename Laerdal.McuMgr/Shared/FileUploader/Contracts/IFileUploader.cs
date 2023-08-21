// ReSharper disable UnusedMember.Global
// ReSharper disable EventNeverSubscribedTo.Global

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Laerdal.McuMgr.Common;
using Laerdal.McuMgr.FileUploader.Contracts.Events;

namespace Laerdal.McuMgr.FileUploader.Contracts
{
    /// <summary>Uploads a file on a specific Nordic-chip-based BLE device</summary>
    /// <remarks>For the file-uploading process to even commence you need to be authenticated with the AED device that's being targeted.</remarks>
    public interface IFileUploader
    {
        /// <summary>Holds the last error message emitted</summary>
        public string LastFatalErrorMessage { get; }

        /// <summary>Event raised when an error occurs</summary>
        public event EventHandler<FatalErrorOccurredEventArgs> FatalErrorOccurred;

        /// <summary>Event raised when the operation gets cancelled</summary>
        public event EventHandler<CancelledEventArgs> Cancelled;
        
        /// <summary>Event raised when a log gets emitted</summary>
        public event EventHandler<LogEmittedEventArgs> LogEmitted;
        
        /// <summary>Event raised when the file-uploading state changes</summary>
        public event EventHandler<StateChangedEventArgs> StateChanged;
        
        /// <summary>Event raised when the file-uploading busy-state changes which happens when data start or stop being transmitted</summary>
        public event EventHandler<BusyStateChangedEventArgs> BusyStateChanged;
        
        /// <summary>Event raised when the file-uploading process progresses in terms of uploading the firmware files across</summary>
        public event EventHandler<FileUploadProgressPercentageAndDataThroughputChangedEventArgs> FileUploadProgressPercentageAndDataThroughputChanged;

        /// <summary>
        /// Begins the file-uploading process for multiple files. To really know when the upgrade process has been completed you have to register to the events emitted by the uploader.
        /// </summary>
        /// <param name="remoteFilePathsAndTheirDataBytes">The files to upload.</param>
        /// <param name="sleepTimeBetweenRetriesInMs">The time to sleep between each retry after a failed try.</param>
        /// <param name="timeoutPerUploadInMs">The amount of time to wait for each upload to complete before bailing out.</param>
        /// <param name="maxRetriesPerUpload">Maximum amount of retries per upload before bailing out.</param>
        Task UploadAsync(IDictionary<string, byte[]> remoteFilePathsAndTheirDataBytes, int sleepTimeBetweenRetriesInMs = 1_000, int timeoutPerUploadInMs = -1, int maxRetriesPerUpload = 10);

        /// <summary>
        /// Begins the file-uploading process. To really know when the upgrade process has been completed you have to register to the events emitted by the uploader.
        /// </summary>
        /// <param name="localData">The local data to upload.</param>
        /// <param name="remoteFilePath">The remoteFilePath to upload the data to.</param>
        /// <param name="sleepTimeBetweenRetriesInMs">The time to sleep between each retry after a failed try.</param>
        /// <param name="timeoutForUploadInMs">The amount of time to wait for the upload to complete before bailing out.</param>
        /// <param name="maxRetriesCount">Maximum amount of retries before bailing out.</param>
        Task UploadAsync(byte[] localData, string remoteFilePath, int timeoutForUploadInMs = -1, int maxRetriesCount = 10, int sleepTimeBetweenRetriesInMs = 1_000);
        
        /// <summary>
        /// Begins the file-uploading process. To really know when the upgrade process has been completed you have to register to the events emitted by the uploader.
        /// </summary>
        /// <param name="remoteFilePath">The remoteFilePath to upload the data to.</param>
        /// <param name="data">The file-data.</param>
        EFileUploaderVerdict BeginUpload(string remoteFilePath, byte[] data);

        /// <summary>Cancels the file-uploading process</summary>
        void Cancel();
        
        /// <summary>Disconnects the file-uploader from the targeted device</summary>
        void Disconnect();
    }
}
