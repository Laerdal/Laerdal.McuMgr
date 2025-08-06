using System.Threading.Tasks;
using Laerdal.McuMgr.DeviceResetting.Contracts.Enums;

namespace Laerdal.McuMgr.DeviceResetting.Contracts
{
    public interface IDeviceResetterCommandable
    {
        /// <summary>
        /// Starts the resetting process. Basically reboots the device - it doesn't delete any of the firmware or the configuration.
        /// </summary>
        /// <param name="timeoutInMs">The amount of time to wait for the operation to complete before bailing out. If set to zero or negative then the operation will wait indefinitely.</param>
        Task ResetAsync(int timeoutInMs = -1);
        
        /// <summary>Starts the resetting process. Basically reboots the device - it doesn't delete any of the firmware or the configuration.</summary>
        EDeviceResetterInitializationVerdict BeginReset();

        /// <summary>Drops the active bluetooth-connection to the Zephyr device.</summary>
        void Disconnect();
    }
}