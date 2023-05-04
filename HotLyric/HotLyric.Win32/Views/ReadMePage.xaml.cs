using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using HotLyric.Win32.ViewModels;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.System;

namespace HotLyric.Win32.Views
{
    public partial class ReadMePage : Page
    {
        private static HttpClient? client;

        public ReadMePage()
        {
            this.InitializeComponent();
            this.Loaded += ReadMePage_Loaded;

            MarkdownContent.ImageResolving += MarkdownContent_ImageResolving;
            MarkdownContent.LinkClicked += MarkdownContent_LinkClicked;
        }

        public SettingsWindowViewModel VM => (SettingsWindowViewModel)LayoutRoot.DataContext;

        private async void ReadMePage_Loaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            if (client == null)
            {
                client = new HttpClient();
            }

            try
            {
                var text = await client.GetStringAsync("https://raw.githubusercontent.com/cnbluefire/HotLyric/main/README.md");

                MarkdownContent.Text = text;
            }
            finally
            {
                LoadingRing.IsActive = false;
            }
        }

        private void MarkdownContent_ImageResolving(object? sender, CommunityToolkit.WinUI.UI.Controls.ImageResolvingEventArgs e)
        {
            if (e.Url.StartsWith("assets"))
            {
                e.Handled = true;
                e.Image = new BitmapImage(new Uri($"https://raw.githubusercontent.com/cnbluefire/HotLyric/main/{e.Url}"));
            }
        }
        private async void MarkdownContent_LinkClicked(object? sender, CommunityToolkit.WinUI.UI.Controls.LinkClickedEventArgs e)
        {
            if (e.Link == "HotLyric/HotLyric.Package/ThirdPartyNotices.txt")
            {
                VM.ThirdPartyNoticeCmd.Execute(null);
            }
            else if (Uri.TryCreate(e.Link, UriKind.Absolute, out var link))
            {
                await Launcher.LaunchUriAsync(link);
            }
        }

    }
}
