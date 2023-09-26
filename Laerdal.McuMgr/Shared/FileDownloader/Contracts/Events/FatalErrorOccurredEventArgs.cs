// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable ClassNeverInstantiated.Global

using Laerdal.McuMgr.Common.Events;

namespace Laerdal.McuMgr.FileDownloader.Contracts.Events
{
    public readonly struct FatalErrorOccurredEventArgs : IMcuMgrEventArgs
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
