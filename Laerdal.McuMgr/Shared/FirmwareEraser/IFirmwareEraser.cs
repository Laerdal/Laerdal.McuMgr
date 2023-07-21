// ReSharper disable UnusedMember.Global
// ReSharper disable EventNeverSubscribedTo.Global

using System;
using System.Threading.Tasks;
using Laerdal.McuMgr.Common;
using Laerdal.McuMgr.FirmwareEraser.Events;

namespace Laerdal.McuMgr.FirmwareEraser
{
    public interface IFirmwareEraser
    {
        public enum EFirmwareErasureState
        {
            None = 0,
            Idle = 1,
            Erasing = 2,
            Complete = 3
        }
        
        event EventHandler<LogEmittedEventArgs> LogEmitted;
        event EventHandler<StateChangedEventArgs> StateChanged;
        event EventHandler<BusyStateChangedEventArgs> BusyStateChanged;
        event EventHandler<FatalErrorOccurredEventArgs> FatalErrorOccurred;
        
        string LastFatalErrorMessage { get; }

        /// <summary>
        /// Starts the erasure process.
        /// </summary>
        /// <param name="imageIndex">The zero-based index of the firmware image to delete. By default it's 1 which is the index of the non-active image.</param>
        void BeginErasure(int imageIndex = 1);

        /// <summary>Drops the active bluetooth-connection to the Zephyr device.</summary>
        void Disconnect();

        Task EraseAsync(int imageIndex = 1, int timeoutInMs = -1);
    }
}
