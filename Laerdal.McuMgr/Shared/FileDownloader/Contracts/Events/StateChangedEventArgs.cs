// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable ClassNeverInstantiated.Global

using Laerdal.McuMgr.Common.Events;
using Laerdal.McuMgr.FileDownloader.Contracts.Enums;

namespace Laerdal.McuMgr.FileDownloader.Contracts.Events
{
    public readonly struct StateChangedEventArgs : IMcuMgrEventArgs
    {
        public string Resource { get; }
        public EFileDownloaderState NewState { get; }
        public EFileDownloaderState OldState { get; }

        public StateChangedEventArgs(string resource, EFileDownloaderState oldState, EFileDownloaderState newState)
        {
            Resource = resource;
            NewState = newState;
            OldState = oldState;
        }
    }
}
