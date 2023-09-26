// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable ClassNeverInstantiated.Global

using Laerdal.McuMgr.Common.Events;

namespace Laerdal.McuMgr.FileUploader.Contracts.Events
{
    public readonly struct FatalErrorOccurredEventArgs : IMcuMgrEventArgs
    {
        public string ErrorMessage { get; }
        public string RemoteFilePath { get; }
        
        public FatalErrorOccurredEventArgs(string remoteFilePath, string errorMessage)
        {
            ErrorMessage = errorMessage;
            RemoteFilePath = remoteFilePath;
        }
    }
}
