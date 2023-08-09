// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable ClassNeverInstantiated.Global

using System;

namespace Laerdal.McuMgr.DeviceResetter.Contracts.Events
{
    public sealed class StateChangedEventArgs : EventArgs
    {
        public IDeviceResetter.EDeviceResetterState NewState { get; }
        public IDeviceResetter.EDeviceResetterState OldState { get; }

        public StateChangedEventArgs(IDeviceResetter.EDeviceResetterState oldState, IDeviceResetter.EDeviceResetterState newState)
        {
            NewState = newState;
            OldState = oldState;
        }
    }
}
