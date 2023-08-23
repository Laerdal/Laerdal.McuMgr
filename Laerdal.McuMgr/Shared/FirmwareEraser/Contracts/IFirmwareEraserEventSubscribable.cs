using System;
using Laerdal.McuMgr.Common;
using Laerdal.McuMgr.FirmwareEraser.Contracts.Events;

namespace Laerdal.McuMgr.FirmwareEraser.Contracts
{
    public interface IFirmwareEraserEventSubscribable
    {
        /// <summary>Event raised when a log gets emitted</summary>
        event EventHandler<LogEmittedEventArgs> LogEmitted;
        
        /// <summary>Event raised when the state changes</summary>
        event EventHandler<StateChangedEventArgs> StateChanged;
        
        /// <summary>Event raised when the busy-state changes</summary>
        event EventHandler<BusyStateChangedEventArgs> BusyStateChanged;
        
        /// <summary>Event raised when a fatal error occurs</summary>
        event EventHandler<FatalErrorOccurredEventArgs> FatalErrorOccurred;
    }
}