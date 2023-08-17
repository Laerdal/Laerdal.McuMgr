// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable ClassNeverInstantiated.Global

using System;

namespace Laerdal.McuMgr.FileUploader.Contracts.Events
{
    public sealed class FatalErrorOccurredEventArgs : EventArgs
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
