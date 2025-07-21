// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable ClassNeverInstantiated.Global

using Laerdal.McuMgr.Common.Events;

namespace Laerdal.McuMgr.FirmwareInstaller.Contracts.Events
{
    public readonly struct BusyStateChangedEventArgs : IMcuMgrEventArgs //hotpath
    {
        public readonly bool BusyNotIdle;

        public BusyStateChangedEventArgs(bool busyNotIdle)
        {
            BusyNotIdle = busyNotIdle;
        }
    }
}
