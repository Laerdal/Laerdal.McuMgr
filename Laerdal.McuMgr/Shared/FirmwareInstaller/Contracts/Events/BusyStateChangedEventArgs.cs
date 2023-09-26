// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable ClassNeverInstantiated.Global

using Laerdal.McuMgr.Common.Events;

namespace Laerdal.McuMgr.FirmwareInstaller.Contracts.Events
{
    public readonly struct BusyStateChangedEventArgs : IMcuMgrEventArgs
    {
        public bool BusyNotIdle { get; }

        public BusyStateChangedEventArgs(bool busyNotIdle)
        {
            BusyNotIdle = busyNotIdle;
        }
    }
}
