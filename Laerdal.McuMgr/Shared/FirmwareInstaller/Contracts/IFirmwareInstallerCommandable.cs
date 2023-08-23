using System.Threading.Tasks;
using Laerdal.McuMgr.FirmwareInstaller.Contracts.Enums;

namespace Laerdal.McuMgr.FirmwareInstaller.Contracts
{
    public interface IFirmwareInstallerCommandable
    {
        /// <summary>
        /// Begins the firmware upgrade process. To really know when the upgrade process has been completed you have to employ the progressPercentage methods.
        /// </summary>
        /// <param name="data">The firmware bytes. If zipped then the archive must contain the .bin file and not a directory.</param>
        /// <param name="mode">The firmware upgrade mode. Best to leave this to the default value 'TestAndConfirm'.</param>
        /// <param name="eraseSettings">Specifies whether preexisting settings should be erased or not.</param>
        /// <param name="estimatedSwapTimeInMilliseconds">In rF52840, due to how the flash memory works, requires ~20 sec to erase images.
        ///     For Laerdal AEDs the recommended time is ~50secs. Adjust the time accordingly for each device you're testing.</param>
        /// <param name="windowCapacity">Set the window capacity. Values > 1 enable a new implementation for uploading
        ///     the images, which makes use of SMP pipelining feature. The app will send this many packets immediately, without waiting for notification
        ///     confirming each packet. This value should be lower or equal to MCUMGR_BUF_COUNT
        ///     (https://github.com/zephyrproject-rtos/zephyr/blob/bd4ddec0c8c822bbdd420bd558b62c1d1a532c16/subsys/mgmt/mcumgr/Kconfig#L550)
        ///     parameter in KConfig in NCS / Zephyr configuration and should also be supported on Mynewt devices. Mind, that in Zephyr,
        ///     before https://github.com/zephyrproject-rtos/zephyr/pull/41959 was merged, the device required data to be sent with memory alignment.
        ///     Otherwise, the device would ignore uneven bytes and reply with lower than expected offset
        ///     causing multiple packets to be sent again dropping the speed instead of increasing it.</param>
        /// <param name="memoryAlignment">Set the selected memory alignment. Defaults to 4 to match Nordic devices.</param>
        /// <param name="pipelineDepth">If set to a value larger than 1, this enables SMP Pipelining, wherein multiple packets of data ('chunks') are sent at
        ///     once before awaiting a response, which can lead to a big increase in transfer speed if the receiving hardware supports this feature.</param>
        /// <param name="byteAlignment">When PipelineLength is larger than 1 (SMP Pipelining Enabled) it's necessary to set this in order for the stack
        ///     to predict offset jumps as multiple packets are sent in parallel.</param>
        /// <param name="timeoutInMs">The amount of time to wait for the operation to complete before bailing out. If set to zero or negative then the operation will wait indefinitely.</param>
        Task InstallAsync(
            byte[] data,
            EFirmwareInstallationMode mode = EFirmwareInstallationMode.TestAndConfirm,
            bool? eraseSettings = null,
            int? estimatedSwapTimeInMilliseconds = null,
            int? windowCapacity = null,
            int? memoryAlignment = null,
            int? pipelineDepth = null,
            int? byteAlignment = null,
            int timeoutInMs = -1
        );
        
        /// <summary>
        /// Begins the firmware upgrade process. To really know when the upgrade process has been completed you have to employ the progressPercentage methods.
        /// </summary>
        /// <param name="data">The firmware bytes. If zipped then the archive must contain the .bin file and not a directory.</param>
        /// <param name="mode">The firmware upgrade mode. Best to leave this to the default value 'TestAndConfirm'.</param>
        /// <param name="eraseSettings">Specifies whether preexisting settings should be erased or not.</param>
        /// <param name="estimatedSwapTimeInMilliseconds">In rF52840, due to how the flash memory works, requires ~20 sec to erase images.
        /// For Laerdal AEDs the recommended time is ~50secs. Adjust the time accordingly for each device you're testing.</param>
        /// <param name="windowCapacity">Set the window capacity. Values > 1 enable a new implementation for uploading
        ///     the images, which makes use of SMP pipelining feature. The app will send this many packets immediately, without waiting for notification
        ///     confirming each packet. This value should be lower or equal to MCUMGR_BUF_COUNT
        ///     (https://github.com/zephyrproject-rtos/zephyr/blob/bd4ddec0c8c822bbdd420bd558b62c1d1a532c16/subsys/mgmt/mcumgr/Kconfig#L550)
        ///     parameter in KConfig in NCS / Zephyr configuration and should also be supported on Mynewt devices. Mind, that in Zephyr,
        ///     before https://github.com/zephyrproject-rtos/zephyr/pull/41959 was merged, the device required data to be sent with memory alignment.
        ///     Otherwise, the device would ignore uneven bytes and reply with lower than expected offset
        ///     causing multiple packets to be sent again dropping the speed instead of increasing it.</param>
        /// <param name="memoryAlignment">Set the selected memory alignment. Defaults to 4 to match Nordic devices.</param>
        /// <param name="pipelineDepth">If set to a value larger than 1, this enables SMP Pipelining, wherein multiple packets of data ('chunks') are sent at
        /// once before awaiting a response, which can lead to a big increase in transfer speed if the receiving hardware supports this feature.</param>
        /// <param name="byteAlignment">When PipelineLength is larger than 1 (SMP Pipelining Enabled) it's necessary to set this in order for the stack
        /// to predict offset jumps as multiple packets are sent in parallel.</param>
        EFirmwareInstallationVerdict BeginInstallation(
            byte[] data,
            EFirmwareInstallationMode mode = EFirmwareInstallationMode.TestAndConfirm,
            bool? eraseSettings = null,
            int? estimatedSwapTimeInMilliseconds = null,
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