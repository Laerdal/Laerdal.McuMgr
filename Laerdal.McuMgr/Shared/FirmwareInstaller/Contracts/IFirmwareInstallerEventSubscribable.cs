using System;
using Laerdal.McuMgr.Common;
using Laerdal.McuMgr.FirmwareInstaller.Contracts.Events;

namespace Laerdal.McuMgr.FirmwareInstaller.Contracts
{
    public interface IFirmwareInstallerEventSubscribable
    {
        /// <summary>Event raised when a fatal error occurs during firmware installation</summary>
        event EventHandler<FatalErrorOccurredEventArgs> FatalErrorOccurred;

        /// <summary>Event raised when the firmware installation gets cancelled</summary>
        event EventHandler<CancelledEventArgs> Cancelled;

        /// <summary>Event raised when a log gets emitted</summary>
        event EventHandler<LogEmittedEventArgs> LogEmitted;

        /// <summary>Event raised when the firmware installation state changes</summary>
        event EventHandler<StateChangedEventArgs> StateChanged;

        /// <summary>Event raised when the firmware-upgrade busy-state changes which happens when data start or stop being transmitted</summary>
        event EventHandler<BusyStateChangedEventArgs> BusyStateChanged;
        
        /// <summary>Event raised when the firmware-upgrade process progresses in terms of uploading the firmware files across</summary>
        public event EventHandler<FirmwareUploadProgressPercentageAndDataThroughputChangedEventArgs> FirmwareUploadProgressPercentageAndDataThroughputChanged;
    }
}