using System;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace BTFTool
{
    public static class CodeHelper
    {
        public static string Escape(this string input)
        {            
            var sb = new StringBuilder(input.Length+2);
            sb.Append("\"");
            foreach (var c in input)
            {
                switch (c)
                {
                    case '\'': sb.Append(@"\'"); break;
                    case '\"': sb.Append("\\\""); break;
                    case '\\': sb.Append(@"\\"); break;
                    case '\0': sb.Append(@"\0"); break;
                    case '\a': sb.Append(@"\a"); break;
                    case '\b': sb.Append(@"\b"); break;
                    case '\f': sb.Append(@"\f"); break;
                    case '\n': sb.Append(@"\n"); break;
                    case '\r': sb.Append(@"\r"); break;
                    case '\t': sb.Append(@"\t"); break;
                    case '\v': sb.Append(@"\v"); break;
                    default:
                        if (Char.GetUnicodeCategory(c) != UnicodeCategory.Control)
                        {
                            sb.Append(c);
                        }
                        else
                        {
                            sb.Append(@"\u");
                            sb.Append(((ushort)c).ToString("x4"));
                        }
                        break;
                }
            }
            sb.Append("\"");
            return sb.ToString();
        }

        public static string Unescape(this string input)
        {
            input=input.Trim();
            if (input.StartsWith("\"") && input.EndsWith("\"")) input = input.Substring(1, input.Length - 2);
            input = Regex.Unescape(input);
            if (string.IsNullOrWhiteSpace(input)) return null;
            return input;
        }
    }
}
