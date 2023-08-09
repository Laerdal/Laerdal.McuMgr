// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable ClassNeverInstantiated.Global

using System;

namespace Laerdal.McuMgr.DeviceResetter.Contracts.Events
{
    public sealed class FatalErrorOccurredEventArgs : EventArgs
    {
        public string ErrorMessage { get; }
        
        public FatalErrorOccurredEventArgs(string errorMessage)
        {
            ErrorMessage = errorMessage;
        }
    }
}
