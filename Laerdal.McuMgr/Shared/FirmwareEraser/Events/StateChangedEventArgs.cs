// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable ClassNeverInstantiated.Global

using System;
using Laerdal.McuMgr.FirmwareEraser.Contracts;

namespace Laerdal.McuMgr.FirmwareEraser.Events
{
    public sealed class StateChangedEventArgs : EventArgs
    {
        public IFirmwareEraser.EFirmwareErasureState NewState { get; }
        public IFirmwareEraser.EFirmwareErasureState OldState { get; }

        public StateChangedEventArgs(IFirmwareEraser.EFirmwareErasureState oldState, IFirmwareEraser.EFirmwareErasureState newState)
        {
            NewState = newState;
            OldState = oldState;
        }
    }
}
