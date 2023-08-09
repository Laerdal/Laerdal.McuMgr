// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable ClassNeverInstantiated.Global

using System;

namespace Laerdal.McuMgr.DeviceResetter.Contracts.Events
{
    public sealed class StateChangedEventArgs : EventArgs
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
