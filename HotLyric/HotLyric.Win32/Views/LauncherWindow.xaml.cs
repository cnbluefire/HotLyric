using HotLyric.Win32.Utils;
using HotLyric.Win32.Utils.SystemMediaTransportControls;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Windows.System;

namespace HotLyric.Win32.Views
{
    /// <summary>
    /// LauncherWindow.xaml 的交互逻辑
    /// </summary>
    public partial class LauncherWindow : Window
    {
        public LauncherWindow()
        {
            InitializeComponent();
            this.Icon = WindowHelper.GetDefaultAppIconImage();
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            var hwnd = new WindowInteropHelper(this).Handle;

            WindowHelper.SetWindowIconVisible(hwnd, false);
        }

        private async void OpenHyPlayerButton_Click(object sender, RoutedEventArgs e)
        {
            await LaunchApp(SMTCApps.HyPlayer.PackageFamilyNamePrefix, SMTCApps.HyPlayer.StoreUri);
        }

        private async void OpenLyricEaseButton_Click(object sender, RoutedEventArgs e)
        {
            await LaunchApp(SMTCApps.LyricEase.PackageFamilyNamePrefix, SMTCApps.LyricEase.StoreUri);
        }

        private async Task LaunchApp(string packageFamilyNamePrefix, Uri uri)
        {
            var res = await ApplicationHelper.TryLaunchAppAsync(packageFamilyNamePrefix);

            if (!res) await Launcher.LaunchUriAsync(uri);
        }
    }
}
