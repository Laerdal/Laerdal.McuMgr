// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable ClassNeverInstantiated.Global

namespace Laerdal.McuMgr.FirmwareEraser.Contracts.Events
{
    public readonly struct BusyStateChangedEventArgs
    {
        public bool BusyNotIdle { get; }

        public BusyStateChangedEventArgs(bool busyNotIdle)
        {
            BusyNotIdle = busyNotIdle;
        }
    }
}
