using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace HotLyric.Win32.Utils
{
    public static class HttpClientManager
    {
        private static readonly IReadOnlyList<Uri> TestUris = new[]
        {
            new Uri("https://www.github.com"),
            new Uri("https://raw.githubusercontent.com/"),
            new Uri("https://www.gitee.com"),
            new Uri("https://music.163.com"),
            new Uri("https://c.y.qq.com"),
            new Uri("https://u.y.qq.com"),
            new Uri("https://lyrics.kugou.com"),
        };
        private static HttpClientHandler? handler;
        private static HttpClientProxyConfigure? proxy;
        private static SystemProxy? systemProxy;

        public static HttpClientProxyConfigure? Proxy
        {
            get => proxy;
            set
            {
                if (proxy != value)
                {
                    proxy = value;
                    handler = null;

                    Lyricify.Lyrics.Providers.Web.BaseApi.HttpClient = CreateClient();
                }
            }
        }

        public static HttpClient CreateClient()
        {
            lock (TestUris)
            {
                var proxy = HttpClientManager.proxy;
                if (proxy?.IsDefaultProxy is true)
                {
                    var _systemProxy = SystemProxy.Create();

                    try
                    {
                        if (systemProxy == null)
                        {
                            systemProxy = _systemProxy;
                            _systemProxy = null;
                            handler = null;
                        }
                        else
                        {
                            var collection1 = TestUris.Select(c => systemProxy.GetProxy(c));
                            var collection2 = TestUris.Select(c => _systemProxy.GetProxy(c));
                            if (!Enumerable.SequenceEqual(collection1, collection2))
                            {
                                systemProxy = _systemProxy;
                                _systemProxy = null;
                                handler = null;
                            }
                        }
                    }
                    finally
                    {
                        _systemProxy?.Dispose();
                    }
                }

                if (handler == null)
                {
                    handler = new HttpClientHandler();
                    if (proxy == null)
                    {
                        handler.UseProxy = false;
                        handler.Proxy = null;
                    }
                    else if (proxy.IsDefaultProxy)
                    {
                        handler.UseProxy = true;
                        handler.Proxy = systemProxy;
                    }
                    else
                    {
                        handler.UseProxy = true;
                        handler.Proxy = new WebProxy(proxy.Host, proxy.Port);
                        if (!string.IsNullOrEmpty(proxy.UserName))
                        {
                            handler.Proxy.Credentials = new NetworkCredential(proxy.UserName, proxy.Password);
                        }
                    }
                }

                return new HttpClient(handler);
            }
        }

        private sealed class SystemProxy : IWebProxy, IDisposable
        {
            private bool disposedValue;
            private IWebProxy internalProxy;

            private SystemProxy(IWebProxy proxy)
            {
                internalProxy = proxy;
            }

            public ICredentials? Credentials
            {
                get
                {
                    ObjectDisposedException.ThrowIf(disposedValue, typeof(SystemProxy));
                    return internalProxy.Credentials;
                }
                set
                {
                    ObjectDisposedException.ThrowIf(disposedValue, typeof(SystemProxy));
                    internalProxy.Credentials = value;
                }
            }

            public Uri? GetProxy(Uri destination)
            {
                ObjectDisposedException.ThrowIf(disposedValue, typeof(SystemProxy));
                return internalProxy.GetProxy(destination);
            }

            public bool IsBypassed(Uri host)
            {
                ObjectDisposedException.ThrowIf(disposedValue, typeof(SystemProxy));
                return internalProxy.IsBypassed(host);
            }

            public void Dispose()
            {
                if (!disposedValue)
                {
                    disposedValue = true;
                    if (internalProxy is IDisposable disposable)
                    {
                        internalProxy = null!;
                        disposable.Dispose();
                    }
                    else
                    {
                        internalProxy = null!;
                    }
                }
            }

            public static SystemProxy Create()
            {
                return new SystemProxy(ConstructSystemProxy());
            }

            #region Factory

            private static bool notSupported = false;
            private static object staticLocker = new object();
            private static Type? systemProxyInfoType;
            private static MethodInfo? constructSystemProxyMethodInfo;

            [DynamicDependency("ConstructSystemProxy", "System.Net.Http.SystemProxyInfo", "System.Net.Http")]
            private static IWebProxy ConstructSystemProxy()
            {
                if (notSupported) return new HttpNoProxy();

                if (systemProxyInfoType == null)
                {
                    lock (staticLocker)
                    {
                        if (systemProxyInfoType == null)
                        {
                            systemProxyInfoType = Type.GetType("System.Net.Http.SystemProxyInfo, System.Net.Http");
                            if (systemProxyInfoType != null)
                            {
                                constructSystemProxyMethodInfo = systemProxyInfoType.GetMethod("ConstructSystemProxy", BindingFlags.Static | BindingFlags.Public);
                            }
                        }
                    }
                }

                if (constructSystemProxyMethodInfo == null)
                {
                    notSupported = true;
                    return new HttpNoProxy();
                }
                if (constructSystemProxyMethodInfo.Invoke(null, null) is IWebProxy proxy)
                {
                    return proxy;
                }
                return new HttpNoProxy();
            }

            #endregion Factory
        }

        private sealed class HttpNoProxy : IWebProxy
        {
            public ICredentials? Credentials { get; set; }
            public Uri? GetProxy(Uri destination) => null;
            public bool IsBypassed(Uri host) => true;
        }
    }

    public record HttpClientProxyConfigure(string? Host, int Port, string? UserName, string? Password)
    {
        public bool IsDefaultProxy
        {
            [MemberNotNullWhen(false, "Host")]
            get => string.IsNullOrEmpty(Host);
        }
    }
}
