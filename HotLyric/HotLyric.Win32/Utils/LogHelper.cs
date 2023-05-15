using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace HotLyric.Win32.Utils
{
    public static class LogHelper
    {
        public static NLog.Logger Logger =>
            NLog.LogManager.GetLogger("global");

        public static void LogInfo(string message)
        {
            Logger.Info(message);
        }

        public static void LogError(Exception ex, [CallerMemberName] string? callerName = null, [CallerFilePath] string? callerFilePath = null, [CallerLineNumber] int callerLineNumber = 0)
        {
            Logger.Error(ex, BuildMessage(null, callerName, callerFilePath, callerLineNumber));
        }

        public static void LogError(string message, Exception ex, [CallerMemberName] string? callerName = null, [CallerFilePath] string? callerFilePath = null, [CallerLineNumber] int callerLineNumber = 0)
        {
            Logger.Error(ex, BuildMessage(message, callerName, callerFilePath, callerLineNumber));
        }

        private static string? BuildMessage(string? message, string? callerName, string? callerFilePath, int callerLineNumber)
        {
            var path = "";

            if (!string.IsNullOrEmpty(callerFilePath))
            {
                var idx = callerFilePath.IndexOf("HotLyric.Win32");
                idx += "HotLyric.Win32".Length;

                while (idx < callerFilePath.Length)
                {
                    var ch = callerFilePath[idx];
                    if (ch != '/' && ch != '\\')
                    {
                        break;
                    }
                    idx++;
                }

                if (idx < callerFilePath.Length)
                {
                    path = callerFilePath.Substring(idx);
                }
            }

            var sb = new StringBuilder(200);

            if (!string.IsNullOrEmpty(path))
            {
                sb.Append(path).Append(' ');
            }
            if (!string.IsNullOrEmpty(callerName))
            {
                sb.Append('(')
                    .Append(callerName)
                    .Append(')')
                    .Append(' ');
            }
            if (callerLineNumber > 0 && !string.IsNullOrEmpty(path))
            {
                sb.Append("#")
                    .Append(callerLineNumber)
                    .Append(' ');
            }

            if (!string.IsNullOrEmpty(message))
            {
                sb.Append("Message: ")
                    .Append(message);
            }

            return sb.ToString();
        }
    }
}
