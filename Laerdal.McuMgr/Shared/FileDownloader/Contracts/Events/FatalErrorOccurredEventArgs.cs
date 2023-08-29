// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable ClassNeverInstantiated.Global

namespace Laerdal.McuMgr.FileDownloader.Contracts.Events
{
    public readonly struct FatalErrorOccurredEventArgs
    {
        public string Resource { get; }
        public string ErrorMessage { get; }
        
        public FatalErrorOccurredEventArgs(string resource, string errorMessage)
        {
            Resource = resource;
            ErrorMessage = errorMessage;
        }
    }
}
