using HotLyric.Win32.Utils;
using HotLyric.Win32.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WinUIEx;

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
            VM.HotKeyManager.ResetToDefaultSettings();
        }
    }
}
