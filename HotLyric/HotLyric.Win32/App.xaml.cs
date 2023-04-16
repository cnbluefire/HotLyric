using HotLyric.Win32.Base;
using HotLyric.Win32.Controls;
using HotLyric.Win32.Utils;
using HotLyric.Win32.ViewModels;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Vanara.PInvoke;
using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;
using Microsoft.UI.Xaml;
using WinUIEx;
using Microsoft.UI.Dispatching;
using HotLyric.Win32.Base.BackgroundHelpers;
using HotLyric.Win32.Views;

namespace HotLyric.Win32
{
    sealed partial class App : Application
    {
        private NotifyIconHelper? notifyIcon;

        public App()
        {
            this.UnhandledException += App_UnhandledException;
            TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
            DispatcherQueue = DispatcherQueue.GetForCurrentThread();

            InitializeComponent();
        }

        protected override void OnLaunched(LaunchActivatedEventArgs args)
        {
            notifyIcon = new NotifyIconHelper();

            CreateMainWindow();

            //_ = CheckUpdateAsync();

            ViewModelLocator.Instance.SettingsWindowViewModel.TryShowReadMeOnStartup();
        }


        public static new App Current => (App)Application.Current;

        public static DispatcherQueue DispatcherQueue { get; private set; } = null!;

        internal Views.LyricView? LyricView { get; private set; }

        internal Views.SettingsView? SettingsView { get; set; }

        internal NotifyIconHelper? NotifyIcon => notifyIcon;

        public bool Exiting { get; private set; }

        private void CreateMainWindow()
        {
            LyricView = new Views.LyricView();

            LyricView.Show();
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
                    App.DispatcherQueue.TryEnqueue(() =>
                    {
                        _ = updateResult.TryStartUpdateAsync();
                        var vm = ViewModelLocator.Instance.SettingsWindowViewModel;
                        vm.OpenStorePageCmd.Execute(null);
                    });
                };

                notifier.Show(toast);
            }
        }

        private void App_UnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
        {
            LogException(e.Exception);
        }
        private void TaskScheduler_UnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
        {
            LogException(e.Exception);
        }

        private static void LogException(object exception)
        {
            if (exception is Exception ex)
            {
                NLog.LogManager.GetCurrentClassLogger().Error(ex, "App_UnhandledException");
                Environment.Exit(ex.HResult);
            }
        }


        public new void Exit()
        {
            try
            {
                Exiting = true;

                LyricView?.Close();
                LyricView = null;

                ViewModelLocator.Instance.LyricWindowViewModel.SelectedSession = null;
                var array = ViewModelLocator.Instance.LyricWindowViewModel.SessionModels?.ToArray();
                ViewModelLocator.Instance.LyricWindowViewModel.SessionModels = null;

                if (array != null)
                {
                    foreach (var item in array)
                    {
                        item.Dispose();
                    }
                }
            }
            finally
            {
                base.Exit();
            }
        }


        internal static void Instance_Activated(object? sender, Microsoft.Windows.AppLifecycle.AppActivationArguments e)
        {
            _ = DispatcherQueue?.TryEnqueue(() =>
            {
                ActivationArgumentsHelper.ProcessArguments(e);

                ViewModelLocator.Instance.SettingsWindowViewModel.ActivateInstance();
            });
        }
    }
}
