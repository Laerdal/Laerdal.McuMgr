using System;
using Laerdal.McuMgr.Common.Enums;

namespace Laerdal.McuMgr.Common
{
    static internal class HelpersIOS
    {
        static internal ELogLevel TranslateEIOSLogLevel(string level) => level?.Trim().ToUpperInvariant() switch
        {
            "D" => ELogLevel.Debug,
            "V" => ELogLevel.Verbose,
            "I" => ELogLevel.Info,
            "A" => ELogLevel.Info, //application
            "W" => ELogLevel.Warning,
            "E" => ELogLevel.Error,
            _ => throw new ArgumentOutOfRangeException(nameof(level), level, "Unknown log-level value")
        };
    }
}
