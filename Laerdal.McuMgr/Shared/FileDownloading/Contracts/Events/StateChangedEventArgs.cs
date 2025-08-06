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
        public string Resource { get; init; }
        public EFileDownloaderState NewState { get; init; }
        public EFileDownloaderState OldState { get; init; }

        public StateChangedEventArgs(string resource, EFileDownloaderState oldState, EFileDownloaderState newState)
        {
            Resource = resource;
            NewState = newState;
            OldState = oldState;
        }
    }
}
