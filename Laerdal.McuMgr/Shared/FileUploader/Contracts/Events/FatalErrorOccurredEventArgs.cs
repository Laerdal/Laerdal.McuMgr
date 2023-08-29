// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable ClassNeverInstantiated.Global

namespace Laerdal.McuMgr.FileUploader.Contracts.Events
{
    public readonly struct FatalErrorOccurredEventArgs
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
