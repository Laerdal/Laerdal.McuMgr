// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable ClassNeverInstantiated.Global

using System.Runtime.InteropServices;
using Laerdal.McuMgr.Common.Events;
using Laerdal.McuMgr.FileDownloading.Contracts.Enums;

namespace Laerdal.McuMgr.FileDownloading.Contracts.Events
{
    [StructLayout(LayoutKind.Auto)]
    public readonly struct StateChangedEventArgs : IMcuMgrEventArgs
    {
        public string RemoteFilePath { get; init; }
        public EFileDownloaderState NewState { get; init; }
        public EFileDownloaderState OldState { get; init; }

        public StateChangedEventArgs(string remoteFilePath, EFileDownloaderState oldState, EFileDownloaderState newState)
        {
            NewState = newState;
            OldState = oldState;
            RemoteFilePath = remoteFilePath;
        }
    }
}
