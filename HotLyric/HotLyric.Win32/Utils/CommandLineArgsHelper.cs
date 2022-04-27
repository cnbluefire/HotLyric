using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Web;

namespace HotLyric.Win32.Utils
{
    public static class CommandLineArgsHelper
    {
        private static string? launchFromPackageFamilyName;

        public static DateTimeOffset LaunchFromPackageFamilyNameLastChange { get; private set; }

        public static string? LaunchFromPackageFamilyName
        {
            get => launchFromPackageFamilyName;
            set
            {
                if (launchFromPackageFamilyName != value)
                {
                    launchFromPackageFamilyName = value;
                    LaunchFromPackageFamilyNameLastChange = DateTimeOffset.Now;
                }
            }
        }

        public static bool HasLaunchParameters
        {
            get
            {
                var lastChange = LaunchFromPackageFamilyNameLastChange;
                var diffSeconds = (DateTimeOffset.Now - lastChange).TotalSeconds;

                if (diffSeconds < 10) return true;

                return !string.IsNullOrEmpty(launchFromPackageFamilyName) && diffSeconds < 5 * 60;
            }
        }

        public static void ProcessCommandLineArgs(string[] args)
        {
            if (args.Length > 0)
            {
                if (args[0].StartsWith("hot-lyric:", StringComparison.OrdinalIgnoreCase))
                {
                    // 通过协议
                    if (Uri.TryCreate(args[0], UriKind.RelativeOrAbsolute, out var uri))
                    {
                        var query = uri.Query;
                        if (!string.IsNullOrEmpty(query))
                        {
                            NameValueCollection? queryCollection = null;
                            try
                            {
                                queryCollection = HttpUtility.ParseQueryString(query);
                            }
                            catch { }

                            if (queryCollection != null)
                            {

                                if (queryCollection.Get("restart") is string str
                                    && bool.TryParse(str, out var v)
                                    && v)
                                {
                                    System.Threading.Thread.Sleep(1000);
                                }
                                LaunchFromPackageFamilyName = queryCollection.Get("from");
                            }
                        }
                    }
                }
            }
        }

        public static void ProcessCopyDataMessage(string message)
        {
            if (ApplicationHelper.RestartRequested) return;

            if (string.IsNullOrEmpty(message)) return;

            if (message.StartsWith(SingleInstance.ActivateMessage))
            {
                var argsString = message.Substring(SingleInstance.ActivateMessage.Length);
                if (!string.IsNullOrEmpty(argsString))
                {
                    var args = argsString.Split(' ')
                        .Select(c => c.Trim(' ').Trim('\"', '\''))
                        .Where(c => !string.IsNullOrEmpty(c))
                        .ToArray();

                    if (args.Length > 0)
                    {
                        ProcessCommandLineArgs(args);
                    }
                }

                ActivateMainInstanceEventReceived?.Invoke(null, EventArgs.Empty);
            }
        }

        public static event EventHandler? ActivateMainInstanceEventReceived;
    }
}
