using System;

namespace Laerdal.McuMgr.Common
{
    static internal class HelpersAndroid
    {
        static public ELogLevel TranslateEAndroidLogLevel(string level) => level switch //derived from sl4j https://www.slf4j.org/api/org/apache/log4j/Level.html
        {
            "DEBUG" => ELogLevel.Debug,
            "TRACE" => ELogLevel.Verbose,
            "INFO" => ELogLevel.Info,
            "FATAL" => ELogLevel.Error,
            "WARN" => ELogLevel.Warning,
            "ERROR" => ELogLevel.Error,
            _ => throw new ArgumentOutOfRangeException(nameof(level), level, "Unknown enum value")
        };
    }
}
