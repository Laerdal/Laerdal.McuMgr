using System.Threading.Tasks;
using Laerdal.McuMgr.DeviceResetter.Contracts.Enums;

namespace Laerdal.McuMgr.DeviceResetter.Contracts
{
    public interface IDeviceResetterCommands
    {
        /// <summary>Returns the last fatal error message that was emitted by the device-resetter.</summary>
        string LastFatalErrorMessage { get; }

        /// <summary>Returns the current state of the device-resetter.</summary>
        EDeviceResetterState State { get; }

        /// <summary>
        /// Starts the resetting process. Basically reboots the device - it doesn't delete any of the firmware or the configuration.
        /// </summary>
        /// <param name="timeoutInMs">The amount of time to wait for the operation to complete before bailing out. If set to zero or negative then the operation will wait indefinitely.</param>
        Task ResetAsync(int timeoutInMs = -1);
        
        /// <summary>Starts the resetting process. Basically reboots the device - it doesn't delete any of the firmware or the configuration.</summary>
        void BeginReset();

        /// <summary>Drops the active bluetooth-connection to the Zephyr device.</summary>
        void Disconnect();
    }
}