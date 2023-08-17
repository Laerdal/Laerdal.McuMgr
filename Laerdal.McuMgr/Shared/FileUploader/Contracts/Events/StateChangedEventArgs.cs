// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable ClassNeverInstantiated.Global

using System;

namespace Laerdal.McuMgr.FileUploader.Contracts.Events
{
    public sealed class StateChangedEventArgs : EventArgs
    {
        public string RemoteFilePath { get; }
        public IFileUploader.EFileUploaderState NewState { get; }
        public IFileUploader.EFileUploaderState OldState { get; }

        public StateChangedEventArgs(string remoteFilePath, IFileUploader.EFileUploaderState oldState, IFileUploader.EFileUploaderState newState)
        {
            NewState = newState;
            OldState = oldState;
            RemoteFilePath = remoteFilePath;
        }
    }
}
