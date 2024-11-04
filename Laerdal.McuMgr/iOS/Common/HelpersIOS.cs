using System;
using Laerdal.McuMgr.Common.Enums;

namespace Laerdal.McuMgr.Common
{
    static internal class HelpersIOS
    {
        static internal ELogLevel TranslateEIOSLogLevel(string level) => level?.Trim().ToUpperInvariant() switch
        {
            "T" or "TRACE" => ELogLevel.Trace,
            "D" or "DEBUG" => ELogLevel.Debug,
            "V" or "VERBOSE" => ELogLevel.Verbose,
            "I" or "INFO" => ELogLevel.Info,
            "N" or "NOTICE" => ELogLevel.Info,
            "A" or "APPLICATION" => ELogLevel.Info, //application
            "W" or "WARN" => ELogLevel.Warning,
            "E" or "ERROR" => ELogLevel.Error,
            "C" or "CRITICAL" => ELogLevel.Error,
            _ => throw new ArgumentOutOfRangeException(nameof(level), level, "Unknown log-level value")
        };
    }
}
