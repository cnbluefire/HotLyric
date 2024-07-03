using BlueFire.Toolkit.WinUI3.Extensions;
using HotLyric.Win32.Controls;
using HotLyric.Win32.Models;
using HotLyric.Win32.Utils;
using HotLyric.Win32.ViewModels;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HotLyric.Win32.Views
{
    public partial class MiscSettingsPage : Page
    {
        public MiscSettingsPage()
        {
            this.InitializeComponent();
        }

        public SettingsWindowViewModel VM => (SettingsWindowViewModel)DataContext;

        private void ResetWindowBoundsButton_Click(object sender, RoutedEventArgs e)
        {
            if (App.Current.LyricView != null)
            {
                WindowBoundsHelper.ResetWindowBounds(App.Current.LyricView.GetWindowHandle());
                ViewModelLocator.Instance.SettingsWindowViewModel.ActivateInstance();
            }
        }

        private void ResetHotKeyButton_Click(object sender, RoutedEventArgs e)
        {
            VM.HotKeyModels.ResetToDefaultSettings();
        }

        public string MapProxyModelToString(HttpClientProxyModel? proxyModel)
        {
            if (proxyModel == null || proxyModel.IsNoProxy) return "不使用代理";
            else if (proxyModel.IsDefaultProxy) return "使用系统代理";
            return "自定义代理";
        }
    }
}
