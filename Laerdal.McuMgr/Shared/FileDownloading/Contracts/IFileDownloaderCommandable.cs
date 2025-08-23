using System.Collections.Generic;
using System.Threading.Tasks;
using Laerdal.McuMgr.FileDownloading.Contracts.Enums;
using Laerdal.McuMgr.FileDownloading.Contracts.Exceptions;

namespace Laerdal.McuMgr.FileDownloading.Contracts
{
    public interface IFileDownloaderCommandable
    {
        /// <summary>
        /// Begins the file-downloading process on multiple files. Files that cannot be downloaded due to errors will have a null entry in the returned dictionary. To really know when the upgrade process has been completed you have to register to the events emitted by the downloader.
        /// </summary>
        /// <param name="remoteFilePaths">The remote files to download.</param>
        /// <param name="hostDeviceModel">The device-model of the host-device</param>
        /// <param name="hostDeviceManufacturer">The manufacturer of the host-device</param>
        /// <param name="timeoutPerDownloadInMs">The amount of time to wait for each download to complete before skipping it.</param>
        /// <param name="maxTriesPerDownload">The maximum amount of tries per download before skipping and moving over to the next download.</param>
        /// <param name="sleepTimeBetweenRetriesInMs">The amount of time to sleep between retries.</param>
        /// <param name="initialMtuSize">Set the initial MTU size for the connection employed by the firmware-installation (on Android this is useful to deal with
        ///     some problematic devices such as Samsung A8 tablets). On Android acceptable custom values must lay within the range [23, 517] and if the value provided
        ///     is null, zero or negative it will default to 498. Note that in quirky devices like Samsung Galaxy A8 the only value that works is 23 - anything else fails.
        ///     If null or negative it will default to the maximum-write-value-length-for-no-response for the underlying device (in iOS the default value is 57).</param>
        /// <param name="windowCapacity">(Android only) Set the window capacity. Values > 1 enable a new implementation for uploading
        ///     the images, which makes use of SMP pipelining feature. The app will send this many packets immediately, without waiting for notification
        ///     confirming each packet. This value should be lower than or equal to MCUMGR_BUF_COUNT
        ///     (https://github.com/zephyrproject-rtos/zephyr/blob/bd4ddec0c8c822bbdd420bd558b62c1d1a532c16/subsys/mgmt/mcumgr/Kconfig#L550)
        ///     parameter in KConfig in NCS / Zephyr configuration and should also be supported on Mynewt devices. Mind, that in Zephyr,
        ///     before https://github.com/zephyrproject-rtos/zephyr/pull/41959 was merged, the device required data to be sent with memory alignment.
        ///     Otherwise, the device would ignore uneven bytes and reply with lower than expected offset
        ///     causing multiple packets to be sent again dropping the speed instead of increasing it.</param>
        /// <param name="memoryAlignment">(Android only) Set the selected memory alignment. Defaults to 4 to match Nordic devices.</param>
        /// <returns>A dictionary containing the bytes of each remote file that got fetched over.</returns>
        Task<IDictionary<string, byte[]>> DownloadAsync(
            IEnumerable<string> remoteFilePaths,
            string hostDeviceModel,
            string hostDeviceManufacturer,
            int timeoutPerDownloadInMs = -1,
            int maxTriesPerDownload = 10,
            int sleepTimeBetweenRetriesInMs = 0,
            int? initialMtuSize = null,
            int? windowCapacity = null,
            int? memoryAlignment = null
        );

        /// <summary>
        /// Begins the file-downloading process. To really know when the upgrade process has been completed you have to register to the events emitted by the downloader.
        /// </summary>
        /// <param name="remoteFilePath">The remote file to download.</param>
        /// <param name="hostDeviceModel">The device-model of the host-device</param>
        /// <param name="hostDeviceManufacturer">The manufacturer of the host-device</param>
        /// <param name="timeoutForDownloadInMs">The amount of time to wait for the operation to complete before bailing out.</param>
        /// <param name="maxTriesCount">The maximum amount of tries before bailing out with <see cref="AllDownloadAttemptsFailedException"/>.</param>
        /// <param name="sleepTimeBetweenRetriesInMs">The amount of time to sleep between retries.</param>
        /// <param name="gracefulCancellationTimeoutInMs">The time to wait (in milliseconds) for a cancellation request to be properly handled. If this timeout expires then the mechanism will bail out forcefully without waiting for the underlying native code to cleanup properly.</param>
        /// <param name="initialMtuSize">Set the initial MTU size for the connection employed by the firmware-installation (on Android this is useful to deal with
        ///     some problematic devices such as Samsung A8 tablets). On Android acceptable custom values must lay within the range [23, 517] and if the value provided
        ///     is null, zero or negative it will default to 498. Note that in quirky devices like Samsung Galaxy A8 the only value that works is 23 - anything else fails.
        ///     If null or negative it will default to the maximum-write-value-length-for-no-response for the underlying device (in iOS the default value is 57).</param>
        /// <param name="windowCapacity">(Android only) Set the window capacity. Values > 1 enable a new implementation for uploading
        ///     the images, which makes use of SMP pipelining feature. The app will send this many packets immediately, without waiting for notification
        ///     confirming each packet. This value should be lower than or equal to MCUMGR_BUF_COUNT
        ///     (https://github.com/zephyrproject-rtos/zephyr/blob/bd4ddec0c8c822bbdd420bd558b62c1d1a532c16/subsys/mgmt/mcumgr/Kconfig#L550)
        ///     parameter in KConfig in NCS / Zephyr configuration and should also be supported on Mynewt devices. Mind, that in Zephyr,
        ///     before https://github.com/zephyrproject-rtos/zephyr/pull/41959 was merged, the device required data to be sent with memory alignment.
        ///     Otherwise, the device would ignore uneven bytes and reply with lower than expected offset
        ///     causing multiple packets to be sent again dropping the speed instead of increasing it.</param>
        /// <returns>The bytes of the remote file that got fetched over.</returns>
        Task<byte[]> DownloadAsync(
            string remoteFilePath,
            string hostDeviceModel,
            string hostDeviceManufacturer,
            int timeoutForDownloadInMs = -1,
            int maxTriesCount = 10,
            int sleepTimeBetweenRetriesInMs = 1_000,
            int gracefulCancellationTimeoutInMs = 2_500,
            int? initialMtuSize = null,
            int? windowCapacity = null
        );

        /// <summary>
        /// Begins the file-downloading process. To really know when the upgrade process has been completed you have to register to the events emitted by the downloader.
        /// </summary>
        /// <param name="remoteFilePath">The remote file to download.</param>
        /// <param name="hostDeviceModel">The device-model of the host-device</param>
        /// <param name="hostDeviceManufacturer">The manufacturer of the host-device</param>
        /// <param name="initialMtuSize">Set the initial MTU size for the connection employed by the firmware-installation (on Android this is useful to deal with
        ///     some problematic devices such as Samsung A8 tablets). On Android acceptable custom values must lay within the range [23, 517] and if the value provided
        ///     is null, zero or negative it will default to 498. Note that in quirky devices like Samsung Galaxy A8 the only value that works is 23 - anything else fails.
        ///     If null or negative it will default to the maximum-write-value-length-for-no-response for the underlying device (in iOS the default value is 57).</param>
        /// <param name="windowCapacity">(Android only) Set the window capacity. Values > 1 enable a new implementation for uploading
        ///     the images, which makes use of SMP pipelining feature. The app will send this many packets immediately, without waiting for notification
        ///     confirming each packet. This value should be lower than or equal to MCUMGR_BUF_COUNT
        ///     (https://github.com/zephyrproject-rtos/zephyr/blob/bd4ddec0c8c822bbdd420bd558b62c1d1a532c16/subsys/mgmt/mcumgr/Kconfig#L550)
        ///     parameter in KConfig in NCS / Zephyr configuration and should also be supported on Mynewt devices. Mind, that in Zephyr,
        ///     before https://github.com/zephyrproject-rtos/zephyr/pull/41959 was merged, the device required data to be sent with memory alignment.
        ///     Otherwise, the device would ignore uneven bytes and reply with lower than expected offset
        ///     causing multiple packets to be sent again dropping the speed instead of increasing it.</param>
        EFileDownloaderVerdict BeginDownload(
            string remoteFilePath,
            string hostDeviceModel,
            string hostDeviceManufacturer,
            int? initialMtuSize = null,
            int? windowCapacity = null
        );

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
        
        /// <summary>Disconnects the file-downloader from the targeted device</summary>
        bool TryDisconnect();
    }
}