using System.Collections.Generic;
using System.Threading.Tasks;
using Laerdal.McuMgr.Common.Enums;
using Laerdal.McuMgr.FileUploading.Contracts.Exceptions;

namespace Laerdal.McuMgr.FileUploading.Contracts
{
    public interface IFileUploaderCommandable
    {
        static internal class Defaults //@formatter:off
        {
            public const int         MaxTriesPerUpload               = 10;
            public const bool        AutodisposeStreams              = false;
            public const int         TimeoutPerUploadInMs            = -1;
            public const int         SleepTimeBetweenUploadsInMs     = 0;
            public const int         SleepTimeBetweenRetriesInMs     = 100;
            public const bool        MoveToNextUploadInCaseOfError   = true;
            public const int         GracefulCancellationTimeoutInMs = 2_500;

            // DefaultLogLevel = null;
            // PipelineDepth   = null; //these are meant to be left 'null' 
            // ByteAlignment   = null; //so we cannot make these 'const' values
            // InitialMtuSize  = null;
            // WindowCapacity  = null;
            // MemoryAlignment = null;
        } //@formatter:on

        /// <summary>Uploads the given data entries (typically representing the contents of files modeled either as streams or raw byte arrays).</summary>
        /// <remarks>
        /// To really know when the upgrade process has been completed you have to register to the events emitted by the uploader.
        /// <br/><br/>
        /// In case you pass a data-stream into this method then the data-stream will be read immediately into a byte array (from its current position)
        /// but the data-stream will not be disposed of unless the option 'autodisposeStreams' is set explicitly to true.
        /// <br/><br/>
        /// Allowed types for TData are 'Stream', 'Func&lt;Stream&gt;', 'Func&lt;Task&lt;Stream&gt;&gt;', 'Func&lt;ValueTask&lt;Stream&gt;&gt;', 'byte[]' and 'IEnumerable&lt;byte&gt;'.
        /// </remarks>
        /// <param name="remoteFilePathsAndTheirData">The files to upload.</param>
        /// <param name="hostDeviceModel">The device-model of the host-device</param>
        /// <param name="hostDeviceManufacturer">The manufacturer of the host-device</param>
        /// <param name="sleepTimeBetweenUploadsInMs">The time to sleep, in milliseconds, between successful uploads. Defaults to zero.</param>
        /// <param name="sleepTimeBetweenRetriesInMs">The time to sleep, in milliseconds, between each retry after a failed try. Defaults to 100ms.</param>
        /// <param name="gracefulCancellationTimeoutInMs">The time to wait (in milliseconds) for a cancellation request to be properly handled. If this timeout expires then the
        /// mechanism will bail out forcefully without waiting for the underlying native code to cleanup properly.</param>
        /// <param name="timeoutPerUploadInMs">The amount of time to wait for each upload to complete before bailing out.</param>
        /// <param name="maxTriesPerUpload">Maximum amount of tries per upload before bailing out. In case of errors the mechanism will try "maxTriesPerUpload" before bailing out.</param>
        /// <param name="moveToNextUploadInCaseOfError">If set to 'true' (which is the default) the mechanism will move to the next file to upload whenever a particular file fails to be uploaded despite all retries</param>
        /// <param name="autodisposeStreams">If set to 'true' the mechanism will dispose of the data-streams after they have been read into their respective byte arrays (default is 'false').</param>
        /// <param name="minimumNativeLogLevel">The minimum log-level required to greenlit for bubbling up the logs emitted by the uploader during the upload process. Defaults to 'ELogLevel.Error'.</param>
        /// <param name="initialMtuSize">Set the initial MTU size for the connection employed by the firmware-installation (on Android this is useful to deal with
        ///     some problematic devices such as Samsung A8 tablets). On Android acceptable custom values must lay within the range [23, 517] and if the value provided
        ///     is null, zero or negative it will default to 498. Note that in quirky devices like Samsung Galaxy A8 the only value that works is 23 - anything else fails.
        ///     If null or negative it will default to the maximum-write-value-length-for-no-response for the underlying device (in iOS the default value is 57).</param>
        /// <param name="pipelineDepth">(iOS only) If set to a value larger than 1, this enables SMP Pipelining, wherein multiple packets of data ('chunks') are sent at
        ///     once before awaiting a response, which can lead to a big increase in transfer speed if the receiving hardware supports this feature.</param>
        /// <param name="byteAlignment">(iOS only) When PipelineLength is larger than 1 (SMP Pipelining Enabled) it's necessary to set this in order for the stack
        ///     to predict offset jumps as multiple packets are sent in parallel.</param>
        /// <param name="windowCapacity">(Android only) Set the window capacity. Values > 1 enable a new implementation for uploading
        ///     the images, which makes use of SMP pipelining feature. The app will send this many packets immediately, without waiting for notification
        ///     confirming each packet. This value should be lower than or equal to MCUMGR_BUF_COUNT
        ///     (https://github.com/zephyrproject-rtos/zephyr/blob/bd4ddec0c8c822bbdd420bd558b62c1d1a532c16/subsys/mgmt/mcumgr/Kconfig#L550)
        ///     parameter in KConfig in NCS / Zephyr configuration and should also be supported on Mynewt devices. Mind, that in Zephyr,
        ///     before https://github.com/zephyrproject-rtos/zephyr/pull/41959 was merged, the device required data to be sent with memory alignment.
        ///     Otherwise, the device would ignore uneven bytes and reply with lower than expected offset
        ///     causing multiple packets to be sent again dropping the speed instead of increasing it.</param>
        /// <param name="memoryAlignment">(Android only) Set the selected memory alignment. Defaults to 4 to match Nordic devices.</param>
        Task<IEnumerable<string>> UploadAsync<TData>( //@formatter:off
            IDictionary<string, (string ResourceId, TData Data)> remoteFilePathsAndTheirData,
            string hostDeviceModel,
            string hostDeviceManufacturer,
            
            int        sleepTimeBetweenUploadsInMs     = Defaults.SleepTimeBetweenUploadsInMs,
            int        sleepTimeBetweenRetriesInMs     = Defaults.SleepTimeBetweenRetriesInMs,
            int        gracefulCancellationTimeoutInMs = Defaults.GracefulCancellationTimeoutInMs,
            int        timeoutPerUploadInMs            = Defaults.TimeoutPerUploadInMs,
            int        maxTriesPerUpload               = Defaults.MaxTriesPerUpload,
            bool       moveToNextUploadInCaseOfError   = Defaults.MoveToNextUploadInCaseOfError,
            bool       autodisposeStreams              = Defaults.AutodisposeStreams,
            ELogLevel? minimumNativeLogLevel                 = null,
            
            int? initialMtuSize = null,
            int? pipelineDepth = null,
            int? byteAlignment = null,
            int? windowCapacity = null,
            int? memoryAlignment = null
        ); //@formatter:on

        /// <summary>Uploads the given data (typically representing the contents of a file either as a stream or a raw byte array).</summary>
        /// <remarks>
        /// To really know when the upgrade process has been completed you have to register to the events emitted by the uploader.
        /// <br/><br/>
        /// In the case you pass a data-stream into this method then the data-stream will be read immediately into a byte array (from its current position)
        /// but the data-stream will not be disposed of unless the option 'autodisposeStreams' is set explicitly to true.
        /// <br/><br/>
        /// Allowed types for TData are 'Stream', 'Func&lt;Stream&gt;', 'Func&lt;Task&lt;Stream&gt;&gt;', 'Func&lt;ValueTask&lt;Stream&gt;&gt;', 'byte[]' and 'IEnumerable&lt;byte&gt;'.
        /// </remarks>
        /// <param name="localData">The local data to upload.</param>
        /// <param name="resourceId">The id/nickname/filepath of the resource being uploaded</param>
        /// <param name="remoteFilePath">The remote file-path to upload the data to.</param>
        /// <param name="hostDeviceModel">The device-model of the host-device</param>
        /// <param name="hostDeviceManufacturer">The manufacturer of the host-device</param>
        /// <param name="timeoutForUploadInMs">The amount of time to wait for the upload to complete before bailing out.</param>
        /// <param name="maxTriesCount">The maximum amount of tries before bailing out with <see cref="AllFileUploadAttemptsFailedException"/>.</param>
        /// <param name="sleepTimeBetweenRetriesInMs">The time to sleep between each retry after a failed try.</param>
        /// <param name="gracefulCancellationTimeoutInMs">The time to wait (in milliseconds) for a cancellation request to be properly handled. If this timeout expires then the mechanism will bail out forcefully without waiting for the underlying native code to cleanup properly.</param>
        /// <param name="autodisposeStream">If set to 'true' the mechanism will dispose of the data-stream after it has been read into a byte array (default is 'false').</param>
        /// <param name="minimumNativeLogLevel">The minimum log-level required to greenlit for bubbling up the logs emitted by the uploader during the upload process. Defaults to 'ELogLevel.Error'.</param>
        /// <param name="initialMtuSize">Set the initial MTU size for the connection employed by the firmware-installation (on Android this is useful to deal with
        ///     some problematic devices such as Samsung A8 tablets). On Android acceptable custom values must lay within the range [23, 517] and if the value provided
        ///     is null, zero or negative it will default to 498. Note that in quirky devices like Samsung Galaxy A8 the only value that works is 23 - anything else fails.
        ///     If null or negative it will default to the maximum-write-value-length-for-no-response for the underlying device (in iOS the default value is 57).</param>
        /// <param name="pipelineDepth">(iOS only) If set to a value larger than 1, this enables SMP Pipelining, wherein multiple packets of data ('chunks') are sent at
        ///     once before awaiting a response, which can lead to a big increase in transfer speed if the receiving hardware supports this feature.</param>
        /// <param name="byteAlignment">(iOS only) When PipelineLength is larger than 1 (SMP Pipelining Enabled) it's necessary to set this in order for the stack
        ///     to predict offset jumps as multiple packets are sent in parallel.</param>
        /// <param name="windowCapacity">(Android only) Set the window capacity. Values > 1 enable a new implementation for uploading
        ///     the images, which makes use of SMP pipelining feature. The app will send this many packets immediately, without waiting for notification
        ///     confirming each packet. This value should be lower than or equal to MCUMGR_BUF_COUNT
        ///     (https://github.com/zephyrproject-rtos/zephyr/blob/bd4ddec0c8c822bbdd420bd558b62c1d1a532c16/subsys/mgmt/mcumgr/Kconfig#L550)
        ///     parameter in KConfig in NCS / Zephyr configuration and should also be supported on Mynewt devices. Mind, that in Zephyr,
        ///     before https://github.com/zephyrproject-rtos/zephyr/pull/41959 was merged, the device required data to be sent with memory alignment.
        ///     Otherwise, the device would ignore uneven bytes and reply with lower than expected offset
        ///     causing multiple packets to be sent again dropping the speed instead of increasing it.</param>
        /// <param name="memoryAlignment">(Android only) Set the selected memory alignment. Defaults to 4 to match Nordic devices.</param>
        Task UploadAsync<TData>( //@formatter:off
            TData localData,
            string resourceId,
            string remoteFilePath,
            
            string hostDeviceModel,
            string hostDeviceManufacturer,
            
            int         timeoutForUploadInMs            = Defaults.TimeoutPerUploadInMs,
            int         maxTriesCount                   = Defaults.MaxTriesPerUpload,
            int         sleepTimeBetweenRetriesInMs     = Defaults.SleepTimeBetweenRetriesInMs,
            int         gracefulCancellationTimeoutInMs = Defaults.GracefulCancellationTimeoutInMs,
            bool        autodisposeStream               = Defaults.AutodisposeStreams,
            ELogLevel?  minimumNativeLogLevel                 = null,
            
            int? initialMtuSize  = null,
            int? pipelineDepth   = null,
            int? byteAlignment   = null,
            int? windowCapacity  = null,
            int? memoryAlignment = null
        ); //@formatter:on

        /// <summary>
        /// Begins the file-uploading process. To really know when the upgrade process has been completed you have to register to the events emitted by the uploader.
        /// </summary>
        /// <param name="data">The file-data</param>
        /// <param name="resourceId">The id/nickname/filepath of the resource being uploaded</param>
        /// <param name="remoteFilePath">The remote file-path to upload the data to</param>
        /// <param name="hostDeviceModel">The device-model of the host-device</param>
        /// <param name="hostDeviceManufacturer">The manufacturer of the host-device</param>
        /// <param name="minimumNativeLogLevel">The minimum log-level required to greenlit for bubbling up the logs emitted by the uploader during the upload process. Defaults to 'ELogLevel.Error'.</param>
        /// <param name="initialMtuSize">Set the initial MTU size for the connection employed by the firmware-installation (on Android this is useful to deal with
        ///     some problematic devices such as Samsung A8 tablets). On Android acceptable custom values must lay within the range [23, 517] and if the value provided
        ///     is null, zero or negative it will default to 498. Note that in quirky devices like Samsung Galaxy A8 the only value that works is 23 - anything else fails.
        ///     If null or negative it will default to the maximum-write-value-length-for-no-response for the underlying device (in iOS the default value is 57).</param>
        /// <param name="pipelineDepth">(iOS only) If set to a value larger than 1, this enables SMP Pipelining, wherein multiple packets of data ('chunks') are sent at
        ///     once before awaiting a response, which can lead to a big increase in transfer speed if the receiving hardware supports this feature.</param>
        /// <param name="byteAlignment">(iOS only) When PipelineLength is larger than 1 (SMP Pipelining Enabled) it's necessary to set this in order for the stack
        ///     to predict offset jumps as multiple packets are sent in parallel.</param>
        /// <param name="windowCapacity">(Android only) Set the window capacity. Values > 1 enable a new implementation for uploading
        ///     the images, which makes use of SMP pipelining feature. The app will send this many packets immediately, without waiting for notification
        ///     confirming each packet. This value should be lower than or equal to MCUMGR_BUF_COUNT
        ///     (https://github.com/zephyrproject-rtos/zephyr/blob/bd4ddec0c8c822bbdd420bd558b62c1d1a532c16/subsys/mgmt/mcumgr/Kconfig#L550)
        ///     parameter in KConfig in NCS / Zephyr configuration and should also be supported on Mynewt devices. Mind, that in Zephyr,
        ///     before https://github.com/zephyrproject-rtos/zephyr/pull/41959 was merged, the device required data to be sent with memory alignment.
        ///     Otherwise, the device would ignore uneven bytes and reply with lower than expected offset
        ///     causing multiple packets to be sent again dropping the speed instead of increasing it.</param>
        /// <param name="memoryAlignment">(Android only) Set the selected memory alignment. Defaults to 4 to match Nordic devices.</param>
        Task BeginUploadAsync( //@formatter:off
            byte[] data,
            string resourceId,
            string remoteFilePath,
            string hostDeviceModel,
            string hostDeviceManufacturer,
            ELogLevel?  minimumNativeLogLevel = null,
            int?        initialMtuSize  = null,
            int?        pipelineDepth   = null, //  ios
            int?        byteAlignment   = null, //  ios
            int?        windowCapacity  = null, // android
            int?        memoryAlignment = null // android
        ); //@formatter:on

        /// <summary>Sets the minimum log level for the ongoing uploading operation</summary>
        /// <param name="minimumNativeLogLevel">The minimum log level to set</param>
        /// <returns>Always returns true</returns>
        bool TrySetMinimumNativeLogLevel(ELogLevel minimumNativeLogLevel);
        
        /// <summary>
        /// Scraps the current transport. This is useful in case the transport is in a bad state and needs to be restarted.
        /// Method has an effect if and only if the upload has been terminated first (canceled or failed or completed).
        /// </summary>
        /// <returns>True if the transport has been scrapped without issues - False otherwise (which typically means that an upload is still ongoing)</returns>
        bool TryInvalidateCachedInfrastructure();
        
        /// <summary>Sets the context. Mainly needed by Android - this call has no effect in iOS.</summary>
        /// <returns>True if the context was successfully set to the specified one - False otherwise (which typically means that an upload is still ongoing)</returns>
        bool TrySetContext(object context);
        
        /// <summary>Sets the bluetooth device.</summary>
        /// <returns>True if the bluetooth device was successfully set to the specified one - False otherwise (which typically means that an upload is still ongoing)</returns>
        bool TrySetBluetoothDevice(object bluetoothDevice);

        /// <summary>Pauses the file-uploading process</summary>
        /// <returns>True if the pausing request was successfully effectuated (or if the transfer was already paused) - False otherwise which typically means that the underlying transport has been dispoed</returns>
        bool TryPause();
        
        /// <summary>Resumes the file-uploading process</summary>
        /// <returns>True if the resumption request was successfully effectuated (or if the transfer has already been resumed) - False otherwise which typically means is nothing to resume</returns>        
        bool TryResume();
        
        /// <summary>Cancels the file-uploading process</summary>
        /// <param name="reason">(optional) The reason for the cancellation</param>
        /// <returns>True if the cancellation request was successfully sent to the underlying native implementation (or if there is no transfer ongoing to cancel) - False otherwise which typically means there was an internal error (very rare)</returns>
        bool TryCancel(string reason = "");
        
        /// <summary>Disconnects the file-uploader from the targeted device</summary>
        bool TryDisconnect();
    }
}