// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable ClassNeverInstantiated.Global

using System;

namespace Laerdal.McuMgr.FileDownloader.Contracts.Events
{
    public sealed class StateChangedEventArgs : EventArgs
    {
        public EFileDownloaderState NewState { get; }
        public EFileDownloaderState OldState { get; }

        public StateChangedEventArgs(EFileDownloaderState oldState, EFileDownloaderState newState)
        {
            NewState = newState;
            OldState = oldState;
        }
    }
}
