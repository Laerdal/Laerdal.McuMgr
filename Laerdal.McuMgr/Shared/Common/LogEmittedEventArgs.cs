namespace Laerdal.McuMgr.Common
{
    public readonly struct LogEmittedEventArgs
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