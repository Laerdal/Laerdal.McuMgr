// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable ClassNeverInstantiated.Global

using Laerdal.McuMgr.Common.Events;
using Laerdal.McuMgr.FirmwareEraser.Contracts.Enums;

namespace Laerdal.McuMgr.FirmwareEraser.Contracts.Events
{
    public readonly struct StateChangedEventArgs : IMcuMgrEventArgs
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
