// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable ClassNeverInstantiated.Global

using Laerdal.McuMgr.Common.Events;
using Laerdal.McuMgr.DeviceResetter.Contracts.Enums;

namespace Laerdal.McuMgr.DeviceResetter.Contracts.Events
{
    public readonly struct StateChangedEventArgs : IMcuMgrEventArgs
    {
        public EDeviceResetterState NewState { get; }
        public EDeviceResetterState OldState { get; }

        public StateChangedEventArgs(EDeviceResetterState oldState, EDeviceResetterState newState)
        {
            NewState = newState;
            OldState = oldState;
        }
    }
}
