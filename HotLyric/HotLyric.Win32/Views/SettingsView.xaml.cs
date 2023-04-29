using HotLyric.Win32.Base;
using HotLyric.Win32.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Controls;
using WinUIEx;
using Microsoft.UI.Xaml.Media.Animation;
using HotLyric.Win32.Utils;
using Microsoft.UI.Composition;
using WinRT;
using Windows.ApplicationModel;

namespace HotLyric.Win32.Views
{
    public partial class SettingsView : WindowEx
    {
        public SettingsView()
        {
            InitializeComponent();

            ExtendsContentIntoTitleBar = true;
            SetTitleBar(Titlebar);

            if (Microsoft.UI.Composition.SystemBackdrops.MicaController.IsSupported())
            {
                Backdrop = new MicaSystemBackdrop();
            }
            else
            {
                Backdrop = new AcrylicSystemBackdrop()
                {
                    LightTintColor = Windows.UI.Color.FromArgb(0xff, 0xd3, 0xd3, 0xd3),
                    LightFallbackColor = Windows.UI.Color.FromArgb(0xff, 0xd3, 0xd3, 0xd3),
                    LightLuminosityOpacity = 0.95,
                    DarkTintColor = Windows.UI.Color.FromArgb(0xff, 0x54, 0x54, 0x54),
                    DarkFallbackColor = Windows.UI.Color.FromArgb(0xff, 0x54, 0x54, 0x54),
                    DarkLuminosityOpacity = 0.95,
                };

                AcrylicBackground.Opacity = 0.4;
            }

            AppWindow.Closing += AppWindow_Closing;

            _ = InitIconAsync();
        }

        private System.Drawing.Icon? appIcon;

        private async Task InitIconAsync()
        {
            var dpi = this.GetDpiForWindow();

            appIcon = await IconHelper.CreateIconAsync(
                Package.Current.Logo,
                IconHelper.GetSmallIconSize(),
                dpi / 96d,
                default);

            this.SetIcon(appIcon.GetIconId());
        }

        private void AppWindow_Closing(Microsoft.UI.Windowing.AppWindow sender, Microsoft.UI.Windowing.AppWindowClosingEventArgs args)
        {
            args.Cancel = true;
            sender.Hide();
        }

        public SettingsWindowViewModel VM => (SettingsWindowViewModel)LayoutRoot.DataContext;

        private void NavView_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs e)
        {
            if (e.InvokedItem is NavigationViewItem item)
            {

            }
        }

        private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            if (args.SelectedItem is NavigationViewItem item)
            {
                switch (item.Tag as string)
                {
                    case "CommonSettings":
                        ContentFrame.Navigate(typeof(CommonSettingsPage), null, new DrillInNavigationTransitionInfo());
                        break;

                    case "ThemeSettings":
                        ContentFrame.Navigate(typeof(ThemeSettingsPage), null, new DrillInNavigationTransitionInfo());
                        break;

                    case "MiscSettings":
                        ContentFrame.Navigate(typeof(MiscSettingsPage), null, new DrillInNavigationTransitionInfo());
                        break;

                    case "About":
                        ContentFrame.Navigate(typeof(AboutPage), null, new DrillInNavigationTransitionInfo());
                        break;
                }
            }
        }
    }
}
