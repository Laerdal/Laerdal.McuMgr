using Laerdal.McuMgr.Common;
using Laerdal.McuMgr.FirmwareEraser.Contracts.Events;

namespace Laerdal.McuMgr.FirmwareEraser.Contracts
{
    internal interface IFirmwareEraserEventEmitters
    {
        void OnLogEmitted(LogEmittedEventArgs ea);
        void OnStateChanged(StateChangedEventArgs ea);
        void OnBusyStateChanged(BusyStateChangedEventArgs ea);
        void OnFatalErrorOccurred(FatalErrorOccurredEventArgs ea);
    }
}