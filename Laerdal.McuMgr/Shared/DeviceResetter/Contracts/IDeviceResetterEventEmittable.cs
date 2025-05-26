using Laerdal.McuMgr.Common.Contracts;
using Laerdal.McuMgr.DeviceResetter.Contracts.Events;

namespace Laerdal.McuMgr.DeviceResetter.Contracts
{
    internal interface IDeviceResetterEventEmittable : ILogEmittable
    {
        void OnStateChanged(StateChangedEventArgs ea);
        void OnFatalErrorOccurred(FatalErrorOccurredEventArgs ea);
    }
}