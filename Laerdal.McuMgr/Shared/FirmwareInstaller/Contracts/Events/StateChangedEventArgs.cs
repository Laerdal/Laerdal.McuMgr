// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable ClassNeverInstantiated.Global

using System;
using Laerdal.McuMgr.FirmwareInstaller.Contracts.Enums;

namespace Laerdal.McuMgr.FirmwareInstaller.Contracts.Events
{
    public sealed class StateChangedEventArgs : EventArgs
    {
        public EFirmwareInstallationState NewState { get; }
        public EFirmwareInstallationState OldState { get; }

        public StateChangedEventArgs(EFirmwareInstallationState oldState, EFirmwareInstallationState newState)
        {
            NewState = newState;
            OldState = oldState;
        }
    }
}
