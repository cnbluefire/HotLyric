using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HotLyric.Win32.ViewModels;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Navigation;
using Windows.System;

namespace HotLyric.Win32.Views
{
    public partial class ReadMePage : Page
    {
        private static HttpClient? client;
        private static string? readMeContent;
        private CancellationTokenSource? cts;

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

            if (cts == null)
            {
                cts = new CancellationTokenSource();
            }

            try
            {
                MarkdownContent.Text = await GetMarkdownContentAsync(cts.Token);
            }
            catch (OperationCanceledException) { }
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

        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            base.OnNavigatingFrom(e);

            cts?.Cancel();
        }

        private static async Task<string> GetMarkdownContentAsync(CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(readMeContent))
            {
                if (client == null)
                {
                    client = new HttpClient();
                }

                try
                {
                    readMeContent = await client.GetStringAsync("https://raw.githubusercontent.com/cnbluefire/HotLyric/main/README.md", cancellationToken);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    return "加载失败，[前往浏览器查看](https://github.com/cnbluefire/HotLyric/blob/main/README.md)";
                }
            }
            return readMeContent;
        }

    }
}
