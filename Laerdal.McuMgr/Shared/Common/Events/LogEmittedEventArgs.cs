using System.Runtime.InteropServices;
using Laerdal.McuMgr.Common.Enums;

namespace Laerdal.McuMgr.Common.Events
{
    [StructLayout(LayoutKind.Sequential)]
    public readonly struct LogEmittedEventArgs : IMcuMgrEventArgs
    {
        public string Message { get; }
        public string Category { get; }
        public string Resource { get; }
        public ELogLevel Level { get; }

        public LogEmittedEventArgs(string resource, string message, string category, ELogLevel level)
        {
            Level = level;
            Message = message;
            Category = category;
            Resource = resource;
        }
    }
}