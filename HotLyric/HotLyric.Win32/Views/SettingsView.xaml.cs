using HotLyric.Win32.Base;
using HotLyric.Win32.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using HotLyric.Win32.Utils;
using Microsoft.UI.Composition;
using WinRT;
using Windows.ApplicationModel;
using Microsoft.UI.Xaml.Input;
using HotLyric.Win32.Controls;
using Microsoft.UI.Xaml.Media;
using BlueFire.Toolkit.WinUI3;

namespace HotLyric.Win32.Views
{
    public partial class SettingsView : WindowEx
    {
        public SettingsView()
        {
            InitializeComponent();

            XamlWindow.ExtendsContentIntoTitleBar = true;
            XamlWindow.SetTitleBar(Titlebar);

            if (Microsoft.UI.Composition.SystemBackdrops.MicaController.IsSupported())
            {
                SystemBackdrop = new MicaBackdrop();
            }
            else
            {
                SystemBackdrop = new DesktopAcrylicBackdrop();

                AcrylicBackground.Opacity = 0.4;
            }

            AppWindow.Closing += AppWindow_Closing;
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
                NavigateToPageCore(item.Tag as string);
            }
        }

        public void NavigateToPage(string? tag)
        {
            foreach (var item in NavView.MenuItems.OfType<NavigationViewItem>())
            {
                if (item.Tag is string _tag && _tag != tag)
                {
                    item.IsSelected = false;
                }
            }

            NavView.SelectedItem = NavView.MenuItems
                .OfType<NavigationViewItem>()
                .FirstOrDefault(c => c.Tag is string _tag && _tag == tag);
        }

        private void NavigateToPageCore(string? tag)
        {
            switch (tag)
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

                case "ReadMe":
                    ContentFrame.Navigate(typeof(ReadMePage), null, new DrillInNavigationTransitionInfo());
                    break;

                case "About":
                    ContentFrame.Navigate(typeof(AboutPage), null, new DrillInNavigationTransitionInfo());
                    break;
            }
        }
    }
}
