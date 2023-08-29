using Laerdal.McuMgr.Common.Events;
using Laerdal.McuMgr.FirmwareEraser.Contracts.Events;

namespace Laerdal.McuMgr.FirmwareEraser.Contracts
{
    internal interface IFirmwareEraserEventEmittable
    {
        void OnLogEmitted(LogEmittedEventArgs ea);
        void OnStateChanged(StateChangedEventArgs ea);
        void OnBusyStateChanged(BusyStateChangedEventArgs ea);
        void OnFatalErrorOccurred(FatalErrorOccurredEventArgs ea);
    }
}
