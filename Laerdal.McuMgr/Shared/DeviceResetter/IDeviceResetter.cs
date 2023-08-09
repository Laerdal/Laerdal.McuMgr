// ReSharper disable UnusedMember.Global
// ReSharper disable EventNeverSubscribedTo.Global

using System;
using System.Threading.Tasks;
using Laerdal.McuMgr.Common;
using Laerdal.McuMgr.DeviceResetter.Contracts.Events;

namespace Laerdal.McuMgr.DeviceResetter
{
    public interface IDeviceResetter
    {
        public enum EDeviceResetterState
        {
            None = 0,
            Idle = 1,
            Resetting = 2,
            Complete = 3,
            Failed = 4
        }
        
        event EventHandler<LogEmittedEventArgs> LogEmitted;
        event EventHandler<StateChangedEventArgs> StateChanged;
        event EventHandler<FatalErrorOccurredEventArgs> FatalErrorOccurred;

        string LastFatalErrorMessage { get; }

        EDeviceResetterState State { get; }

        /// <summary>
        /// Starts the resetting process. Basically reboots the device - it doesn't delete any of the firmware or the configuration.
        /// </summary>
        /// <param name="timeoutInMs">The amount of time to wait for the operation to complete before bailing out. If set to zero or negative then the operation will wait indefinitely.</param>
        Task ResetAsync(int timeoutInMs = -1);
        
        /// <summary>
        /// Starts the resetting process. Basically reboots the device - it doesn't delete any of the firmware or the configuration.
        /// </summary>
        void BeginReset();

        void Disconnect();
    }
}
