using Laerdal.McuMgr.Common.Contracts;
using Laerdal.McuMgr.Common.Events;
using Laerdal.McuMgr.FirmwareEraser.Contracts.Events;
using Laerdal.McuMgr.FirmwareInstaller.Contracts;

namespace Laerdal.McuMgr.FirmwareEraser.Contracts
{
    internal interface IFirmwareEraserEventEmittable : ILogEmittable
    {
        void OnLogEmitted(LogEmittedEventArgs ea);
        void OnStateChanged(StateChangedEventArgs ea);
        void OnBusyStateChanged(BusyStateChangedEventArgs ea);
        void OnFatalErrorOccurred(FatalErrorOccurredEventArgs ea);
    }
}
