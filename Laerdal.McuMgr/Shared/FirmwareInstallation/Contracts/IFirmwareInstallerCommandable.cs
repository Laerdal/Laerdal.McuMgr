using System.Threading.Tasks;
using Laerdal.McuMgr.Common.Constants;
using Laerdal.McuMgr.Common.Exceptions;
using Laerdal.McuMgr.FirmwareInstallation.Contracts.Enums;
using Laerdal.McuMgr.FirmwareInstallation.Contracts.Exceptions;

namespace Laerdal.McuMgr.FirmwareInstallation.Contracts
{
    public interface IFirmwareInstallerCommandable
    {
        /// <summary>
        /// Begins the firmware upgrade process. To really know when the upgrade process has been completed you have to listen to the associated events of the <see cref="IFirmwareInstallerEventEmittable"/> facade.
        /// </summary>
        /// <remarks>
        /// When 'maxTriesCount' is greater than or equal to 2 the connection will be monitored in terms of how stable and reliable it is during the firmware-uploading stage and if
        /// the uploading phase fails from the third attempt onwards then in the subsequent attempts the fail-safe settings in <see cref="AndroidTidbits.BleConnectionFailsafeSettings.ForUploading"/>
        /// and <see cref="AppleTidbits.BleConnectionFailsafeSettings.ForUploading"/> will be enforced to try to upload the firmware.
        /// </remarks>
        /// <param name="data">The firmware bytes. If zipped then the archive must contain the .bin file and not a directory.</param>
        /// <param name="hostDeviceModel">The model of the host-device.</param>
        /// <param name="hostDeviceManufacturer">The manufacturer of the host-device</param>
        /// <param name="mode">The firmware upgrade mode. Best to leave this to the default value 'TestAndConfirm'.</param>
        /// <param name="eraseSettings">Specifies whether preexisting settings should be erased or not.</param>
        /// <param name="estimatedSwapTimeInMilliseconds">In rF52840, due to how the flash memory works, requires ~20 sec to erase images.
        ///     For Laerdal AEDs the recommended time is ~50secs. Adjust the time accordingly for each device you're testing.</param>
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
        /// <param name="pipelineDepth">(iOS only) If set to a value larger than 1, this enables SMP Pipelining, wherein multiple packets of data ('chunks') are sent at
        ///     once before awaiting a response, which can lead to a big increase in transfer speed if the receiving hardware supports this feature.</param>
        /// <param name="byteAlignment">(iOS only) When PipelineLength is larger than 1 (SMP Pipelining Enabled) it's necessary to set this in order for the stack
        ///     to predict offset jumps as multiple packets are sent in parallel.</param>
        /// <param name="timeoutInMs">The amount of time to wait for the operation to complete before bailing out. If set to zero or negative then the operation will wait indefinitely.</param>
        /// <param name="maxTriesCount">The maximum amount of tries before bailing out with <see cref="FirmwareInstallationErroredOutException"/>.</param>
        /// <param name="sleepTimeBetweenRetriesInMs">The amount of time (in ms) to sleep between retries.</param>
        /// <param name="gracefulCancellationTimeoutInMs">The time to wait (in milliseconds) for a cancellation request to be properly handled. If this timeout expires then the mechanism will bail out forcefully without waiting for the underlying native code to cleanup properly.</param>
        /// <return>A task that you can await on to know when the operation has completed.</return>
        ///
        /// <exception cref="UnauthorizedException"/>If the operation failed because the host-device is not authorized to perform the operation on the remote ble-device.
        /// <exception cref="FirmwareInstallationTimeoutException"/>If the firmware installation took too long to complete and the timeout has expired.
        /// <exception cref="FirmwareInstallationImageSwappingTimedOutException"/>If the confirmation stage has timed out.
        /// <exception cref="AnotherFirmwareInstallationIsAlreadyOngoingException"/>If another firmware installation is already in progress on the remote device.
        /// <exception cref="AllFirmwareInstallationAttemptsFailedException"/>If all the uploading stage has failed.
        /// <exception cref="FirmwareInstallationUnhealthyFirmwareDataGivenException"/>If the bytes of the given firmware are unhealthy.
        /// <exception cref="FirmwareInstallationErroredOutException"/>If the firmware installation has failed for any other reason.
        Task InstallAsync(
            byte[] data,
            string hostDeviceModel,
            string hostDeviceManufacturer,
            EFirmwareInstallationMode mode = EFirmwareInstallationMode.TestAndConfirm,
            bool? eraseSettings = null,
            int? estimatedSwapTimeInMilliseconds = null,
            int? initialMtuSize = null,
            int? windowCapacity = null, //   android only
            int? memoryAlignment = null, //  android only
            int? pipelineDepth = null, //    ios only
            int? byteAlignment = null, //    ios only
            int timeoutInMs = -1,
            int maxTriesCount = 10,
            int sleepTimeBetweenRetriesInMs = 100,
            int gracefulCancellationTimeoutInMs = 2_500
        );

        /// <summary>
        /// Begins the firmware upgrade process. To really know when the upgrade process has been completed you have to employ the progressPercentage methods.
        /// </summary>
        /// <param name="data">The firmware bytes. If zipped then the archive must contain the .bin file and not a directory.</param>
        /// <param name="hostDeviceModel">The model of the host-device.</param>
        /// <param name="hostDeviceManufacturer">The manufacturer of the host-device.</param>
        /// <param name="mode">The firmware upgrade mode. Best to leave this to the default value 'TestAndConfirm'.</param>
        /// <param name="eraseSettings">Specifies whether preexisting settings should be erased or not.</param>
        /// <param name="estimatedSwapTimeInMilliseconds">In rF52840, due to how the flash memory works, requires ~20 sec to erase images.
        ///     For Laerdal AEDs the recommended time is ~50secs. Adjust the time accordingly for each device you're testing.</param>
        /// <param name="initialMtuSize">Set the initial MTU size for the connection employed by the firmware-installation (on Android this is useful to deal with
        ///     some problematic devices such as Samsung A8 tablets). On Android acceptable custom values must lay within the range [23, 517] and if the value provided
        ///     is null, zero or negative it will default to 498. Note that in quirky devices like Samsung Galaxy A8 the only value that works is 23 - anything else fails.
        ///     If null or negative it will default to the maximum-write-value-length-for-no-response for the underlying device (in iOS the default value is 57).</param>
        /// <param name="windowCapacity">(Android only) Set the window capacity. Values > 1 enable a new implementation for uploading
        ///     the images, which makes use of SMP pipelining feature. The app will send this many packets immediately, without waiting for notification
        ///     confirming each packet. This value should be lower or equal to MCUMGR_BUF_COUNT
        ///     (https://github.com/zephyrproject-rtos/zephyr/blob/bd4ddec0c8c822bbdd420bd558b62c1d1a532c16/subsys/mgmt/mcumgr/Kconfig#L550)
        ///     parameter in KConfig in NCS / Zephyr configuration and should also be supported on Mynewt devices. Mind, that in Zephyr,
        ///     before https://github.com/zephyrproject-rtos/zephyr/pull/41959 was merged, the device required data to be sent with memory alignment.
        ///     Otherwise, the device would ignore uneven bytes and reply with lower than expected offset
        ///     causing multiple packets to be sent again dropping the speed instead of increasing it.</param>
        /// <param name="memoryAlignment">(Android only) Set the selected memory alignment. Defaults to 4 to match Nordic devices.</param>
        /// <param name="pipelineDepth">(iOS only) If set to a value larger than 1, this enables SMP Pipelining, wherein multiple packets of data ('chunks') are sent at
        ///     once before awaiting a response, which can lead to a big increase in transfer speed if the receiving hardware supports this feature.</param>
        /// <param name="byteAlignment">(iOS only) When PipelineLength is larger than 1 (SMP Pipelining Enabled) it's necessary to set this in order for the stack
        ///     to predict offset jumps as multiple packets are sent in parallel.</param>
        /// <returns>A verdict that you can use to tell whether the installation has indeed kicked-off or not.</returns>
        EFirmwareInstallationVerdict BeginInstallation(
            byte[] data,
            string hostDeviceModel,
            string hostDeviceManufacturer,
            EFirmwareInstallationMode mode = EFirmwareInstallationMode.TestAndConfirm,
            bool? eraseSettings = null,
            int? estimatedSwapTimeInMilliseconds = null,
            int? initialMtuSize = null,
            int? windowCapacity = null,
            int? memoryAlignment = null,
            int? pipelineDepth = null,
            int? byteAlignment = null
        );

        /// <summary>
        /// Cancels the firmware upgrade process
        /// </summary>
        void Cancel();

        /// <summary>
        /// Disconnects the firmware installer from the targeted device
        /// </summary>
        void Disconnect();
    }
}