// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable ClassNeverInstantiated.Global

using System;
using Laerdal.McuMgr.FirmwareEraser.Contracts.Enums;

namespace Laerdal.McuMgr.FirmwareEraser.Contracts.Events
{
    public sealed class StateChangedEventArgs : EventArgs
    {
        public EFirmwareErasureState NewState { get; }
        public EFirmwareErasureState OldState { get; }

        public StateChangedEventArgs(EFirmwareErasureState oldState, EFirmwareErasureState newState)
        {
            NewState = newState;
            OldState = oldState;
        }
    }
}
