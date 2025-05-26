using Laerdal.McuMgr.Common.Contracts;
using Laerdal.McuMgr.FirmwareEraser.Contracts.Events;

namespace Laerdal.McuMgr.FirmwareEraser.Contracts
{
    internal interface IFirmwareEraserEventEmittable : ILogEmittable
    {
        void OnStateChanged(StateChangedEventArgs ea);
        void OnBusyStateChanged(BusyStateChangedEventArgs ea);
        void OnFatalErrorOccurred(FatalErrorOccurredEventArgs ea);
    }
}
