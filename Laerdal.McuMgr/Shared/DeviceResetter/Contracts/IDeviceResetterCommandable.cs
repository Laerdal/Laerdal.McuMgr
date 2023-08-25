using System.Threading.Tasks;

namespace Laerdal.McuMgr.DeviceResetter.Contracts
{
    public interface IDeviceResetterCommandable
    {
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