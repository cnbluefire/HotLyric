using CommunityToolkit.Mvvm.ComponentModel;
using HotLyric.Win32.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HotLyric.Win32.Models
{
    public partial class HttpClientProxyModel : ObservableObject
    {
        private static HttpClientProxyModel? noProxy;
        private static HttpClientProxyModel? systemProxy;

        private bool isNoProxy = false;

        public static HttpClientProxyModel? NoProxy => (noProxy ??= new HttpClientProxyModel()
        {
            isNoProxy = true
        });
        public static HttpClientProxyModel SystemProxy => (systemProxy ??= new HttpClientProxyModel());


        [ObservableProperty]
        private string? host;

        [ObservableProperty]
        private int port;

        [ObservableProperty]
        private string? userName;

        [ObservableProperty]
        private string? password;

        public bool IsDefaultProxy => string.IsNullOrEmpty(Host);

        public bool IsNoProxy => isNoProxy;

        public HttpClientProxyConfigure? CreateConfigure()
        {
            if (isNoProxy) return null;
            return new HttpClientProxyConfigure(Host, Port, UserName, Password);
        }
    }
}
