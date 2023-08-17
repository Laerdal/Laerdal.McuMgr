// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable ClassNeverInstantiated.Global

using System;

namespace Laerdal.McuMgr.FileUploader.Contracts.Events
{
    public sealed class BusyStateChangedEventArgs : EventArgs
    {
        public bool BusyNotIdle { get; }
        public string RemoteFilePath { get; }

        public BusyStateChangedEventArgs(string remoteFilePath, bool busyNotIdle)
        {
            BusyNotIdle = busyNotIdle;
            RemoteFilePath = remoteFilePath;
        }
    }
}
