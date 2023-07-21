using System;

namespace Laerdal.McuMgr.Common
{
    static internal class StringExtensions
    {
        static public string ReplaceFirst(this string text, string search, string replaceWith)
        {
            var pos = text.IndexOf(search, StringComparison.Ordinal);
            if (pos < 0)
                return text;

            return text.Substring(0, pos) + replaceWith + text.Substring(pos + search.Length);
        }
    }
}