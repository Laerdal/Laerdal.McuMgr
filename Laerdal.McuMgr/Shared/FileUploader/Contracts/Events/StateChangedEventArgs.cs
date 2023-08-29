// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable ClassNeverInstantiated.Global

using Laerdal.McuMgr.Common.Events;
using Laerdal.McuMgr.FileUploader.Contracts.Enums;

namespace Laerdal.McuMgr.FileUploader.Contracts.Events
{
    public readonly struct StateChangedEventArgs : IMcuMgrEventArgs
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
