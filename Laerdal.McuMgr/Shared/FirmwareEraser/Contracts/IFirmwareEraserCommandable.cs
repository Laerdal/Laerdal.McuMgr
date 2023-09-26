using System.Threading.Tasks;

namespace Laerdal.McuMgr.FirmwareEraser.Contracts
{
    public interface IFirmwareEraserCommandable
    {
        /// <summary>Returns the last fatal error message emitted (if any) by the underlying native mechanism.</summary>
        string LastFatalErrorMessage { get; }

        /// <summary>
        /// Starts the erasure process on the firmware-image specified.
        /// </summary>
        /// <param name="imageIndex">The index of the firmware image to erase. Set to 1 by default which is the index of the inactive firmware image on the device.</param>
        /// <param name="timeoutInMs">The amount of time to wait for the operation to complete before bailing out. If set to zero or negative then the operation will wait indefinitely.</param>
        Task EraseAsync(int imageIndex = 1, int timeoutInMs = -1);

        /// <summary>
        /// Starts the erasure process on the firmware-image specified.
        /// </summary>
        /// <param name="imageIndex">The zero-based index of the firmware image to delete. By default it's 1 which is the index of the inactive firmware image.</param>
        void BeginErasure(int imageIndex = 1);

        /// <summary>Drops the active bluetooth-connection to the Zephyr device.</summary>
        void Disconnect();
    }
}