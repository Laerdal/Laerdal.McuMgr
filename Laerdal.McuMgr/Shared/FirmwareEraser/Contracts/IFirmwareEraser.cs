// ReSharper disable UnusedMember.Global
// ReSharper disable EventNeverSubscribedTo.Global

using System;
using System.Threading.Tasks;
using Laerdal.McuMgr.Common;
using Laerdal.McuMgr.FirmwareEraser.Contracts.Events;

namespace Laerdal.McuMgr.FirmwareEraser.Contracts
{
    public interface IFirmwareEraser : IFirmwareEraserEvents, IFirmwareEraserCommands
    {
    }

    public interface IFirmwareEraserCommands
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

    public interface IFirmwareEraserEvents
    {
        event EventHandler<LogEmittedEventArgs> LogEmitted;
        event EventHandler<StateChangedEventArgs> StateChanged;
        event EventHandler<BusyStateChangedEventArgs> BusyStateChanged;
        event EventHandler<FatalErrorOccurredEventArgs> FatalErrorOccurred;
    }

    internal interface IFirmwareEraserEventEmitters
    {
        void OnLogEmitted(LogEmittedEventArgs ea);
        void OnStateChanged(StateChangedEventArgs ea);
        void OnBusyStateChanged(BusyStateChangedEventArgs ea);
        void OnFatalErrorOccurred(FatalErrorOccurredEventArgs ea);
    }
}
