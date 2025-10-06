using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Apps.Utilities.Utils
{
    static public class RegexOptionsUtillity
    {
        public static RegexOptions GetRegexOptions(IEnumerable<string>? flags)
        {
            var options = RegexOptions.None;
            if (flags == null || !flags.Any()) return options;

            foreach (var flag in flags.Select(f => f.Trim()))
            {
                switch (flag.ToLower())
                {
                    case "insensitive":
                        options |= RegexOptions.IgnoreCase;
                        break;
                    case "multiline":
                        options |= RegexOptions.Multiline;
                        break;
                    case "singleline":
                        options |= RegexOptions.Singleline;
                        break;
                    case "right to left":
                        options |= RegexOptions.RightToLeft;
                        break;
                    case "non-capturing":
                        options |= RegexOptions.ExplicitCapture;
                        break;
                    case "no backtracking":
                        options |= RegexOptions.NonBacktracking;
                        break;
                    case "extended":
                        options |= RegexOptions.IgnorePatternWhitespace;
                        break;
                }
            }
            return options;
        }
    }
}
