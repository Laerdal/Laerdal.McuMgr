using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Laerdal.McuMgr.FileUploader.Contracts.Enums;
using Laerdal.McuMgr.FileUploader.Contracts.Exceptions;

namespace Laerdal.McuMgr.FileUploader.Contracts
{
    public interface IFileUploaderCommandable
    {
        /// <summary>Uploads the given data entries (typically representing the contents of files modeled either as streams or raw byte arrays).</summary>
        /// <remarks>
        /// To really know when the upgrade process has been completed you have to register to the events emitted by the uploader.
        /// <br/><br/>
        /// In case you pass a data-stream into this method then the data-stream will be read immediately into a byte array (from its current position)
        /// but the data-stream will not be disposed of unless the option 'autodisposeStreams' is set explicitly to true.
        /// </remarks>
        /// <param name="remoteFilePathsAndTheirData">The files to upload.</param>
        /// <param name="sleepTimeBetweenRetriesInMs">The time to sleep between each retry after a failed try.</param>
        /// <param name="timeoutPerUploadInMs">The amount of time to wait for each upload to complete before bailing out.</param>
        /// <param name="maxTriesPerUpload">Maximum amount of tries per upload before bailing out. In case of errors the mechanism will try "maxTriesPerUpload" before bailing out.</param>
        /// <param name="moveToNextUploadInCaseOfError">If set to 'true' (which is the default) the mechanism will move to the next file to upload whenever a particular file fails to be uploaded despite all retries</param>
        /// <param name="autodisposeStreams">If set to 'true' the mechanism will dispose of the data-streams after they have been read into their respective byte arrays (default is 'false').</param>
        Task<IEnumerable<string>> UploadAsync<TData>(
            IDictionary<string, TData> remoteFilePathsAndTheirData,
            int sleepTimeBetweenRetriesInMs = 100,
            int timeoutPerUploadInMs = -1,
            int maxTriesPerUpload = 10,
            bool moveToNextUploadInCaseOfError = true,
            bool autodisposeStreams = false
        );

        /// <summary>Uploads the given data (typically representing the contents of a file either as a stream or a raw byte array).</summary>
        /// <remarks>
        /// To really know when the upgrade process has been completed you have to register to the events emitted by the uploader.
        /// <br/><br/>
        /// In the case you pass a data-stream into this method then the data-stream will be read immediately into a byte array (from its current position)
        /// but the data-stream will not be disposed of unless the option 'autodisposeStreams' is set explicitly to true.
        /// </remarks>
        /// <param name="localData">The local data to upload.</param>
        /// <param name="remoteFilePath">The remote file-path to upload the data to.</param>
        /// <param name="timeoutForUploadInMs">The amount of time to wait for the upload to complete before bailing out.</param>
        /// <param name="maxTriesCount">The maximum amount of tries before bailing out with <see cref="AllUploadAttemptsFailedException"/>.</param>
        /// <param name="sleepTimeBetweenRetriesInMs">The time to sleep between each retry after a failed try.</param>
        /// <param name="gracefulCancellationTimeoutInMs">The time to wait (in milliseconds) for a cancellation request to be properly handled. If this timeout expires then the mechanism will bail out forcefully without waiting for the underlying native code to cleanup properly.</param>
        /// <param name="autodisposeStream">If set to 'true' the mechanism will dispose of the data-stream after it has been read into a byte array (default is 'false').</param>
        Task UploadAsync<TData>(
            TData localData,
            string remoteFilePath,
            int timeoutForUploadInMs = -1,
            int maxTriesCount = 10,
            int sleepTimeBetweenRetriesInMs = 1_000,
            int gracefulCancellationTimeoutInMs = 2_500,
            bool autodisposeStream = false
        );
        
        /// <summary>
        /// Begins the file-uploading process. To really know when the upgrade process has been completed you have to register to the events emitted by the uploader.
        /// </summary>
        /// <param name="remoteFilePath">The remote file-path to upload the data to.</param>
        /// <param name="data">The file-data.</param>
        EFileUploaderVerdict BeginUpload(string remoteFilePath, byte[] data);
        
        /// <summary>
        /// Scraps the current transport. This is useful in case the transport is in a bad state and needs to be restarted.
        /// Method has an effect if and only if the upload has been terminated first (canceled or failed or completed).
        /// </summary>
        /// <returns>True if the transport has been scrapped without issues - False otherwise (which typically means that an upload is still ongoing)</returns>
        bool InvalidateCachedTransport();

        /// <summary>Cancels the file-uploading process</summary>
        void Cancel();
        
        /// <summary>Disconnects the file-uploader from the targeted device</summary>
        void Disconnect();
    }
}