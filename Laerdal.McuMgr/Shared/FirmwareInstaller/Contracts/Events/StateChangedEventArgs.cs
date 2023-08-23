// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable ClassNeverInstantiated.Global

using System;

namespace Laerdal.McuMgr.FirmwareInstaller.Contracts.Events
{
    public sealed class StateChangedEventArgs : EventArgs
    {
        public IFirmwareInstaller.EFirmwareInstallationState NewState { get; }
        public IFirmwareInstaller.EFirmwareInstallationState OldState { get; }

        public StateChangedEventArgs(IFirmwareInstaller.EFirmwareInstallationState oldState, IFirmwareInstaller.EFirmwareInstallationState newState)
        {
            NewState = newState;
            OldState = oldState;
        }
    }
}
