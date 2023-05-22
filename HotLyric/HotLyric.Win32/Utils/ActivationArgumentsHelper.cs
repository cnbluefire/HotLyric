using HotLyric.Win32.ViewModels;
using Microsoft.Windows.AppLifecycle;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Web;
using Windows.ApplicationModel.Activation;

namespace HotLyric.Win32.Utils
{
    public static class ActivationArgumentsHelper
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

        public static bool RedirectMode { get; private set; }

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

        public static void ProcessArguments(AppActivationArguments? args)
        {
            switch (args?.Kind)
            {
                case ExtendedActivationKind.Protocol:
                    {
                        if (args.Data is ProtocolActivatedEventArgs protocolArgs)
                        {
                            ProcessUri(protocolArgs.Uri);
                        }
                    }
                    break;


                case ExtendedActivationKind.ProtocolForResults:
                    {
                        if (args.Data is ProtocolForResultsActivatedEventArgs protocolArgs)
                        {
                            ProcessUri(protocolArgs.Uri);
                        }
                    }
                    break;
            }

            void ProcessUri(Uri _uri)
            {
                var query = _uri.Query;
                if (!string.IsNullOrEmpty(query))
                {
                    NameValueCollection? queryCollection = null;
                    try
                    {
                        queryCollection = HttpUtility.ParseQueryString(query);
                    }
                    catch (Exception ex)
                    {
                        HotLyric.Win32.Utils.LogHelper.LogError(ex);
                    }

                    if (queryCollection != null)
                    {
                        if (queryCollection.Get("restart") is string str
                            && bool.TryParse(str, out var v)
                            && v)
                        {
                            System.Threading.Thread.Sleep(1000);
                        }
                        LaunchFromPackageFamilyName = queryCollection.Get("from");

                        if (queryCollection.Get("redirect") is string redirect)
                        {
                            if (string.IsNullOrEmpty(redirect)
                                || string.Equals(redirect, "off", StringComparison.OrdinalIgnoreCase))
                            {
                                RedirectMode = false;
                            }
                            else
                            {
                                RedirectMode = true;
                            }
                        }

                        ActivateMainInstanceEventReceived?.Invoke(null, EventArgs.Empty);
                    }
                }
            }
        }

        public static event EventHandler? ActivateMainInstanceEventReceived;
    }
}
