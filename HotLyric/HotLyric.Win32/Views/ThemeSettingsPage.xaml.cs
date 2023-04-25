using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HotLyric.Win32.Models;
using HotLyric.Win32.ViewModels;
using Microsoft.UI.Xaml.Controls;
using Windows.System;

namespace HotLyric.Win32.Views
{
    public partial class ThemeSettingsPage : Page
    {
        public ThemeSettingsPage()
        {
            this.InitializeComponent();

            this.Loaded += (s, a) =>
            {
                ThemeModeComboBox.SelectedIndex = VM.ThemeIsPresetVisible ? 0 : 1;
            };
        }

        private SettingsWindowViewModel VM => (SettingsWindowViewModel)DataContext;

        private void ThemeModeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (IsLoaded)
            {
                VM.ThemeIsPresetVisible = ThemeModeComboBox.SelectedIndex == 0;
            }
        }

        private void ThemePresetsGridView_ItemClick(object sender, ItemClickEventArgs e)
        {
            if (e.ClickedItem is LyricThemeView model)
            {
                VM.CurrentTheme = model;
            }
        }

        private async void Border_Tapped(object sender, Microsoft.UI.Xaml.Input.TappedRoutedEventArgs e)
        {
            try
            {
                await Launcher.LaunchUriAsync(new Uri("https://go.microsoft.com/fwlink/?linkid=2185388"));
            }
            catch { }  
        }
    }
}
