using Laerdal.McuMgr.Common;
using Laerdal.McuMgr.DeviceResetter.Contracts.Events;

namespace Laerdal.McuMgr.DeviceResetter.Contracts
{
    internal interface IDeviceResetterEventEmitters
    {
        void OnLogEmitted(LogEmittedEventArgs ea);
        void OnStateChanged(StateChangedEventArgs ea);
        void OnFatalErrorOccurred(FatalErrorOccurredEventArgs ea);
    }
}