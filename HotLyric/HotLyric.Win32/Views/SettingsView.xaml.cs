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

            if (Environment.OSVersion.Version >= new Version(10, 0, 22000, 0))
            {
                Backdrop = new MicaSystemBackdrop();
                //IsMicaEnabled = true;
            }
            else
            {
                Backdrop = new AcrylicSystemBackdrop()
                {
                    LightTintOpacity = 0.6,
                    DarkTintOpacity = 0.6
                };
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
