// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable ClassNeverInstantiated.Global

using System;

namespace Laerdal.McuMgr.FileDownloader.Contracts.Events
{
    public sealed class FatalErrorOccurredEventArgs : EventArgs
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
