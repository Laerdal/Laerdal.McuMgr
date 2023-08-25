using System;

namespace Laerdal.McuMgr.FirmwareInstaller.Contracts.Events
{
    public sealed class WarningEventArgs : EventArgs
    {
        public string WarningMessage { get; }
        
        public WarningEventArgs(string warningMessage)
        {
            WarningMessage = warningMessage;
        }
    }
}