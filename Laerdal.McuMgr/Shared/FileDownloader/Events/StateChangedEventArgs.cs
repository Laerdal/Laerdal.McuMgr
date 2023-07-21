// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable ClassNeverInstantiated.Global

using System;

namespace Laerdal.McuMgr.FileDownloader.Events
{
    public sealed class StateChangedEventArgs : EventArgs
    {
        public IFileDownloader.EFileDownloaderState NewState { get; }
        public IFileDownloader.EFileDownloaderState OldState { get; }

        public StateChangedEventArgs(IFileDownloader.EFileDownloaderState oldState, IFileDownloader.EFileDownloaderState newState)
        {
            NewState = newState;
            OldState = oldState;
        }
    }
}
