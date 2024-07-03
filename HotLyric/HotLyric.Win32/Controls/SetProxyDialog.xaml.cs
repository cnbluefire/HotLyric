using BlueFire.Toolkit.WinUI3.Extensions;
using HotLyric.Win32.Models;
using HotLyric.Win32.Utils;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace HotLyric.Win32.Controls
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class SetProxyDialog : ContentDialog
    {
        public SetProxyDialog(HttpClientProxyModel? proxy)
        {
            this.InitializeComponent();

            if (proxy == null || proxy.IsNoProxy) ProxyTypeComboBox.SelectedIndex = 0;
            else if (proxy.IsDefaultProxy) ProxyTypeComboBox.SelectedIndex = 1;
            else
            {
                ProxyTypeComboBox.SelectedIndex = 2;
                ProxyUrlTextBox.Text = $"{proxy.Host}:{proxy.Port}";
                UserNameTextBox.Text = proxy.UserName ?? "";
                PasswordTextBox.Text = proxy.Password ?? "";
            }

            ProxyTypeComboBox.SelectionChanged += (s, a) =>
            {
                UpdateTextBoxEnabledState();
            };
            UpdateTextBoxEnabledState();
        }

        private void UpdateTextBoxEnabledState()
        {
            var selectedTag = "0";
            if (ProxyTypeComboBox.SelectedItem is ComboBoxItem item)
            {
                selectedTag = item.Tag as string;
            }

            switch (selectedTag)
            {
                case "2":
                    ProxyUrlTextBox.IsEnabled = true;
                    UserNameTextBox.IsEnabled = true;
                    PasswordTextBox.IsEnabled = true;
                    break;

                default:
                    ProxyUrlTextBox.IsEnabled = false;
                    UserNameTextBox.IsEnabled = false;
                    PasswordTextBox.IsEnabled = false;
                    break;
            }
        }

        public HttpClientProxyModel? Proxy
        {
            get
            {
                var selectedTag = "0";
                if (ProxyTypeComboBox.SelectedItem is ComboBoxItem item)
                {
                    selectedTag = item.Tag as string;
                }

                if (selectedTag == "1")
                {
                    return HttpClientProxyModel.SystemProxy;
                }
                else if (selectedTag == "2")
                {
                    var host = string.Empty;
                    int port = 80;
                    var userName = string.Empty;
                    var password = string.Empty;

                    var url = ProxyUrlTextBox.Text;
                    if (!string.IsNullOrEmpty(url))
                    {
                        var idx = url.LastIndexOf(':');
                        if (idx == -1 || idx == 0 || idx == url.Length - 1) host = url;
                        else if (int.TryParse(url.AsSpan(idx + 1), out port))
                        {
                            host = url[..idx];
                        }
                        else
                        {
                            if (host.StartsWith("https")) port = 443;
                            else if (host.StartsWith("http")) port = 80;
                            else if (host.StartsWith("socks5")) port = 1080;
                        }

                        userName = UserNameTextBox.Text;
                        password = PasswordTextBox.Text;

                        return new HttpClientProxyModel()
                        {
                            Host = host,
                            Port = port,
                            UserName = userName,
                            Password = password
                        };
                    }
                }

                return HttpClientProxyModel.NoProxy;
            }
        }
    }
}
