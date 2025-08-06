using System;
using Laerdal.McuMgr.Common.Events;
using Laerdal.McuMgr.Common.Helpers;
using Laerdal.McuMgr.FirmwareInstallation.Contracts.Events;

namespace Laerdal.McuMgr.FirmwareInstallation.Contracts
{
    public interface IFirmwareInstallerEventSubscribable
    {
        /// <summary>Event raised when a fatal error occurs during firmware installation</summary>
        event EventHandler<FatalErrorOccurredEventArgs> FatalErrorOccurred;

        /// <summary>Event raised when the firmware installation gets cancelled</summary>
        event EventHandler<CancelledEventArgs> Cancelled;

        /// <summary>Event raised when a log gets emitted</summary>
        event ZeroCopyEventHelpers.ZeroCopyEventHandler<LogEmittedEventArgs> LogEmitted;

        /// <summary>Event raised when the firmware installation state changes</summary>
        event EventHandler<StateChangedEventArgs> StateChanged;

        /// <summary>Event raised when the firmware-upgrade busy-state changes which happens when data start or stop being transmitted</summary>
        event EventHandler<BusyStateChangedEventArgs> BusyStateChanged;
        
        /// <summary>Event raised when the firmware-upgrade process detects that the firmware that is about to be uploaded is already present on the remote device.</summary>
        event EventHandler<IdenticalFirmwareCachedOnTargetDeviceDetectedEventArgs> IdenticalFirmwareCachedOnTargetDeviceDetected;

        /// <summary>Event raised when the firmware-upgrade process progresses in terms of uploading the firmware files across</summary>
        event EventHandler<FirmwareUploadProgressPercentageAndDataThroughputChangedEventArgs> FirmwareUploadProgressPercentageAndDataThroughputChanged;
    }
}