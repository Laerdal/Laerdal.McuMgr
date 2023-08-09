// ReSharper disable UnusedMember.Global
// ReSharper disable EventNeverSubscribedTo.Global

using System;
using System.Threading.Tasks;
using Laerdal.McuMgr.Common;
using Laerdal.McuMgr.DeviceResetter.Contracts.Events;

namespace Laerdal.McuMgr.DeviceResetter.Contracts
{
    public interface IDeviceResetter : IDeviceResetterEvents, IDeviceResetterCommands
    {
    }
    
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

    public interface IDeviceResetterEvents
    {
        /// <summary>Event that is raised when the device-resetter emits a log-message.</summary>
        event EventHandler<LogEmittedEventArgs> LogEmitted;
        
        /// <summary>Event that is raised when the state of the device-resetter changes.</summary>
        event EventHandler<StateChangedEventArgs> StateChanged;
        
        /// <summary>Event that is raised when a fatal error occurs.</summary>
        event EventHandler<FatalErrorOccurredEventArgs> FatalErrorOccurred;
    }

    internal interface IDeviceResetterEventEmitters
    {
        void OnLogEmitted(LogEmittedEventArgs ea);
        void OnStateChanged(StateChangedEventArgs ea);
        void OnFatalErrorOccurred(FatalErrorOccurredEventArgs ea);
    }
}
