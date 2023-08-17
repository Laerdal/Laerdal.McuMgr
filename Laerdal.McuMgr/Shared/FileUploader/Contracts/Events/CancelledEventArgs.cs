// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable ClassNeverInstantiated.Global

using System;

namespace Laerdal.McuMgr.FileUploader.Contracts.Events
{
    public sealed class CancelledEventArgs : EventArgs
    {
        public string RemoteFilePath { get; }

        public CancelledEventArgs(string remoteFilePath)
        {
            RemoteFilePath = remoteFilePath;
        }
    }
}