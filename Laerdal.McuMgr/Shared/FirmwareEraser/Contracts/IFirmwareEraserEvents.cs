using System;
using Laerdal.McuMgr.Common;
using Laerdal.McuMgr.FirmwareEraser.Contracts.Events;

namespace Laerdal.McuMgr.FirmwareEraser.Contracts
{
    public interface IFirmwareEraserEvents
    {
        event EventHandler<LogEmittedEventArgs> LogEmitted;
        event EventHandler<StateChangedEventArgs> StateChanged;
        event EventHandler<BusyStateChangedEventArgs> BusyStateChanged;
        event EventHandler<FatalErrorOccurredEventArgs> FatalErrorOccurred;
    }
}