// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable ClassNeverInstantiated.Global

using System;

namespace Laerdal.McuMgr.FileUploader.Contracts.Events
{
    public sealed class StateChangedEventArgs : EventArgs
    {
        public string Resource { get; }
        public EFileUploaderState NewState { get; }
        public EFileUploaderState OldState { get; }

        public StateChangedEventArgs(string resource, EFileUploaderState oldState, EFileUploaderState newState)
        {
            Resource = resource;
            NewState = newState;
            OldState = oldState;
        }
    }
}
