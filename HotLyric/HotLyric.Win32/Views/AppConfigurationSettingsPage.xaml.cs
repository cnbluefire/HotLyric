using HotLyric.Win32.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace HotLyric.Win32.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class AppConfigurationSettingsPage : Page
    {
        public AppConfigurationSettingsPage()
        {
            this.InitializeComponent();
        }

        public AppConfigurationSettingsViewModel VM => (AppConfigurationSettingsViewModel)LayoutRoot.DataContext;

        private async void CopyButton_Click(object sender, RoutedEventArgs e)
        {
            var animation = (Storyboard)((Button)sender).Resources["CopiedAnimation"];
            if (animation.GetCurrentState() != ClockState.Stopped)
            {
                animation.Stop();
            }

            ((Button)sender).IsEnabled = false;

            for (int i = 0; i < 3; i++)
            {
                try
                {
                    if (((Button)sender).Tag is string text)
                    {
                        var dataPackage = new DataPackage();
                        dataPackage.RequestedOperation = DataPackageOperation.Copy;
                        dataPackage.SetText(text);
                        Clipboard.SetContent(dataPackage);
                        Clipboard.Flush();

                        animation.Begin();
                    }
                    break;
                }
                catch
                {
                    await Task.Delay(100);
                }
            }

            ((Button)sender).IsEnabled = true;
        }
    }
}
