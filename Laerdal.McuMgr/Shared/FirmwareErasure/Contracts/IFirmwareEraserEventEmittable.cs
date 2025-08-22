using Laerdal.McuMgr.Common.Contracts;
using Laerdal.McuMgr.FirmwareErasure.Contracts.Events;

namespace Laerdal.McuMgr.FirmwareErasure.Contracts
{
    internal interface IFirmwareEraserEventEmittable : ILogEmittable
    {
        void OnStateChanged(StateChangedEventArgs ea);
        void OnBusyStateChanged(BusyStateChangedEventArgs ea);
        void OnFatalErrorOccurred(FatalErrorOccurredEventArgs ea);
    }
}
