// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable ClassNeverInstantiated.Global

using Laerdal.McuMgr.Common.Events;
using Laerdal.McuMgr.FirmwareErasure.Contracts.Enums;

namespace Laerdal.McuMgr.FirmwareErasure.Contracts.Events
{
    public readonly struct StateChangedEventArgs : IMcuMgrEventArgs
    {
        public EFirmwareErasureState NewState { get; init; }
        public EFirmwareErasureState OldState { get; init; }

        public StateChangedEventArgs(EFirmwareErasureState oldState, EFirmwareErasureState newState)
        {
            NewState = newState;
            OldState = oldState;
        }
    }
}
