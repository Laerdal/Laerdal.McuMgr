using Laerdal.McuMgr.Common.Contracts;
using Laerdal.McuMgr.DeviceResetting.Contracts.Events;

namespace Laerdal.McuMgr.DeviceResetting.Contracts
{
    internal interface IDeviceResetterEventEmittable : ILogEmittable
    {
        void OnStateChanged(StateChangedEventArgs ea);
        void OnFatalErrorOccurred(FatalErrorOccurredEventArgs ea);
    }
}