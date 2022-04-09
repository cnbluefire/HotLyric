using HotLyric.Win32.Controls;
using HotLyric.Win32.Utils;
using HotLyric.Win32.Views;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace HotLyric.Win32
{

    /// <summary>
    /// App.xaml 的交互逻辑
    /// </summary>
    public partial class App : Application
    {
        private HostWindow? lyricHostWindow;
        private NotifyIconHelper? notifyIcon;

        public NotifyIconHelper? NotifyIcon => notifyIcon;

        public static new App Current => (App)Application.Current;

        protected override void OnStartup(StartupEventArgs e)
        {
            CommandLineArgsHelper.ProcessCommandLineArgs(e.Args.Take(1).ToArray());
            base.OnStartup(e);

            Utils.DispatcherHelper.Initialize(Dispatcher);
            Utils.ForegroundWindowHelper.Initialize();

            try
            {
                global::Windows.UI.Notifications.ToastNotificationManager.History.Clear();
            }
            catch { }

            Log($"args: {string.Join(", ", e.Args)}");

            notifyIcon = new NotifyIconHelper();
            lyricHostWindow = new HostWindow();
            lyricHostWindow.Show();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            Utils.ForegroundWindowHelper.Uninitialize();
            Input.MouseManager.Uninstall();

            notifyIcon?.Dispose();
            notifyIcon = null;

            base.OnExit(e);
        }

        public static void Log(string msg)
        {
            if (string.IsNullOrEmpty(msg)) return;

            var msg2 = $"[{DateTime.Now}]{msg}";

            var fileName = "AAALog.log";
            try
            {
                using (var sw = System.IO.File.AppendText(System.IO.Path.Combine(global::Windows.Storage.ApplicationData.Current.LocalCacheFolder.Path, fileName)))
                {
                    sw.WriteLine(msg2);
                }
            }
            catch { }
        }
    }
}
