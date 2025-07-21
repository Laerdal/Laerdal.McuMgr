// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable ClassNeverInstantiated.Global

using Laerdal.McuMgr.Common.Events;
using Laerdal.McuMgr.FirmwareInstaller.Contracts.Enums;

namespace Laerdal.McuMgr.FirmwareInstaller.Contracts.Events
{
    public readonly struct StateChangedEventArgs : IMcuMgrEventArgs
    {
        public EFirmwareInstallationState NewState { get; init; }
        public EFirmwareInstallationState OldState { get; init; }

        public StateChangedEventArgs(EFirmwareInstallationState oldState, EFirmwareInstallationState newState)
        {
            NewState = newState;
            OldState = oldState;
        }
    }
}
