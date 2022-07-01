using HotLyric.Win32.Controls;
using HotLyric.Win32.Utils;
using HotLyric.Win32.ViewModels;
using HotLyric.Win32.Views;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;

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

            notifyIcon = new NotifyIconHelper();
            lyricHostWindow = new HostWindow();
            lyricHostWindow.Show();

            _ = CheckUpdateAsync();

            ViewModelLocator.Instance.SettingsWindowViewModel.TryShowReadMeOnStartup();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            try
            {
                var manager = ToastNotificationManager.GetDefault();
                manager.History.Clear();
            }
            catch { }

            Utils.ForegroundWindowHelper.Uninitialize();
            Input.MouseManager.Uninstall();

            notifyIcon?.Dispose();
            notifyIcon = null;

            base.OnExit(e);
        }

        private async Task CheckUpdateAsync()
        {
            var updateResult = await ApplicationHelper.CheckUpdateAsync();
            if (updateResult.HasUpdate)
            {
                var notifier = ToastNotificationManager.CreateToastNotifier();

                var content = @"<toast>
  
  <visual>
    <binding template=""ToastGeneric"">
      <text>发现新版本</text>
      <text>点击前往商店查看</text>
    </binding>
  </visual>
  
</toast>";
                var xml = new XmlDocument();
                xml.LoadXml(content);
                var toast = new ToastNotification(xml);
                toast.Activated += (s, a) =>
                {
                    DispatcherHelper.UIDispatcher?.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(() =>
                    {
                        _ = updateResult.TryStartUpdateAsync();
                        var vm = ViewModelLocator.Instance.SettingsWindowViewModel;
                        vm.OpenStorePageCmd.Execute(null);
                    }));
                };

                notifier.Show(toast);
            }
        }
    }
}
