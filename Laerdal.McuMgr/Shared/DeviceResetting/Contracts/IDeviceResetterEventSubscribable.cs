using System;
using Laerdal.McuMgr.Common.Events;
using Laerdal.McuMgr.Common.Helpers;
using Laerdal.McuMgr.DeviceResetting.Contracts.Events;

namespace Laerdal.McuMgr.DeviceResetting.Contracts
{
    public interface IDeviceResetterEventSubscribable
    {
        /// <summary>Event that is raised when the device-resetter emits a log-message.</summary>
        event ZeroCopyEventHelpers.ZeroCopyEventHandler<LogEmittedEventArgs> LogEmitted;

        /// <summary>Event that is raised when the state of the device-resetter changes.</summary>
        event EventHandler<StateChangedEventArgs> StateChanged;
        
        /// <summary>Event that is raised when a fatal error occurs.</summary>
        event EventHandler<FatalErrorOccurredEventArgs> FatalErrorOccurred;
    }
}