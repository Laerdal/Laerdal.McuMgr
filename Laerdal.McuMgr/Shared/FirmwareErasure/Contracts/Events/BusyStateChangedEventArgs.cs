// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable ClassNeverInstantiated.Global

using Laerdal.McuMgr.Common.Events;

namespace Laerdal.McuMgr.FirmwareErasure.Contracts.Events
{
    public readonly struct BusyStateChangedEventArgs : IMcuMgrEventArgs
    {
        public bool BusyNotIdle { get; init; }

        public BusyStateChangedEventArgs(bool busyNotIdle)
        {
            BusyNotIdle = busyNotIdle;
        }
    }
}
