using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Laerdal.McuMgr.FileUploader.Contracts.Events;

namespace Laerdal.McuMgr.FileUploader.Contracts
{
    public interface IFileUploaderCommands
    {
        /// <summary>Holds the last error message emitted</summary>
        public string LastFatalErrorMessage { get; }

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