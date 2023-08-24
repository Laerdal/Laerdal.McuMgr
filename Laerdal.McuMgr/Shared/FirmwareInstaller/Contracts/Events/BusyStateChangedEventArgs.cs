// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable ClassNeverInstantiated.Global

using System;

namespace Laerdal.McuMgr.FirmwareInstaller.Contracts.Events
{
    public sealed class BusyStateChangedEventArgs : EventArgs
    {
        public bool BusyNotIdle { get; }

        public BusyStateChangedEventArgs(bool busyNotIdle)
        {
            BusyNotIdle = busyNotIdle;
        }
    }
}