using System.Collections.Generic;
using System.Threading.Tasks;
using Laerdal.McuMgr.FileUploader.Contracts.Enums;
using Laerdal.McuMgr.FileUploader.Contracts.Exceptions;

namespace Laerdal.McuMgr.FileUploader.Contracts
{
    public interface IFileUploaderCommandable
    {
        /// <summary>
        /// Begins the file-uploading process for multiple files.
        /// 
        /// To really know when the upgrade process has been completed you have to register to the events emitted by the uploader.
        /// </summary>
        /// <param name="remoteFilePathsAndTheirDataBytes">The files to upload.</param>
        /// <param name="sleepTimeBetweenRetriesInMs">The time to sleep between each retry after a failed try.</param>
        /// <param name="timeoutPerUploadInMs">The amount of time to wait for each upload to complete before bailing out.</param>
        /// <param name="maxTriesPerUpload">Maximum amount of retries per upload before bailing out. In case of errors the mechanism will try "1 + maxTriesPerUpload" before bailing out.</param>
        /// <param name="moveToNextUploadInCaseOfError">If set to 'true' (which is the default) the mechanism will move to the next file to upload whenever a particular file fails to be uploaded despite all retries</param>
        Task<IEnumerable<string>> UploadAsync(
            IDictionary<string, byte[]> remoteFilePathsAndTheirDataBytes,
            int sleepTimeBetweenRetriesInMs = 100,
            int timeoutPerUploadInMs = -1,
            int maxTriesPerUpload = 10,
            bool moveToNextUploadInCaseOfError = true
        );

        /// <summary>
        /// Begins the file-uploading process for a single file.
        ///
        /// To really know when the upgrade process has been completed you have to register to the events emitted by the uploader.
        /// </summary>
        /// <param name="localData">The local data to upload.</param>
        /// <param name="remoteFilePath">The remote file-path to upload the data to.</param>
        /// <param name="timeoutForUploadInMs">The amount of time to wait for the upload to complete before bailing out.</param>
        /// <param name="maxTriesCount">The maximum amount of retries before bailing out with <see cref="AllUploadAttemptsFailedException"/>.</param>
        /// <param name="sleepTimeBetweenRetriesInMs">The time to sleep between each retry after a failed try.</param>
        /// <param name="gracefulCancellationTimeoutInMs">The time to wait (in milliseconds) for a cancellation request to be properly handled. If this timeout expires then the mechanism will bail out forcefully without waiting for the underlying native code to cleanup properly.</param>
        Task UploadAsync(
            byte[] localData,
            string remoteFilePath,
            int timeoutForUploadInMs = -1,
            int maxTriesCount = 10,
            int sleepTimeBetweenRetriesInMs = 1_000,
            int gracefulCancellationTimeoutInMs = 2_500
        );
        
        /// <summary>
        /// Begins the file-uploading process. To really know when the upgrade process has been completed you have to register to the events emitted by the uploader.
        /// </summary>
        /// <param name="remoteFilePath">The remote file-path to upload the data to.</param>
        /// <param name="data">The file-data.</param>
        EFileUploaderVerdict BeginUpload(string remoteFilePath, byte[] data);

        /// <summary>Cancels the file-uploading process</summary>
        void Cancel();
        
        /// <summary>Disconnects the file-uploader from the targeted device</summary>
        void Disconnect();
    }
}