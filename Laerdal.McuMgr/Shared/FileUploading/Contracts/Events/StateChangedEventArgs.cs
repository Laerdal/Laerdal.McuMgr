// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable ClassNeverInstantiated.Global

using System.Runtime.InteropServices;
using Laerdal.McuMgr.Common.Events;
using Laerdal.McuMgr.FileUploading.Contracts.Enums;

namespace Laerdal.McuMgr.FileUploading.Contracts.Events
{
    [StructLayout(LayoutKind.Auto)]
    public readonly struct StateChangedEventArgs : IMcuMgrEventArgs
    {
        public string Resource { get; init; }
        public EFileUploaderState NewState { get; init; }
        public EFileUploaderState OldState { get; init; }

        public StateChangedEventArgs(string resource, EFileUploaderState oldState, EFileUploaderState newState)
        {
            Resource = resource;
            NewState = newState;
            OldState = oldState;
        }
    }
}
