using CommunityToolkit.Mvvm.ComponentModel;
using DirectN;
using HotLyric.Win32.Models.AppConfigurationModels;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;
using static ICSharpCode.SharpZipLib.Zip.ExtendedUnixData;

namespace HotLyric.Win32.Utils.AppConfigurations
{
    public class AppConfigurationManager
    {
        private static readonly IReadOnlyList<string> DefaultUris = [
            "https://raw.githubusercontent.com/cnbluefire/HotLyric.Configuration/main/configuration.json",
            "https://gitee.com/blue-fire/HotLyric.Configuration/raw/main/configuration.json"
        ];

        private SemaphoreSlim sourceLocker = new SemaphoreSlim(1, 1);
        private SemaphoreSlim configLocker = new SemaphoreSlim(1, 1);
        private SemaphoreSlim updateConfigLocker = new SemaphoreSlim(1, 1);

        private CancellationTokenSource? cancellationTokenSource;
        private AppConfigurationModelResult? currentConfiguration;

        #region Sources Manager

        /// <summary>
        /// 获取所有更新源
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<AppConfigurationSourceModel[]> GetSourcesAsync(CancellationToken cancellationToken = default)
        {
            await sourceLocker.WaitAsync(cancellationToken);

            try
            {
                return await GetSourcesAsyncCore(cancellationToken);
            }
            finally
            {
                sourceLocker.Release();
            }
        }

        private async Task<AppConfigurationSourceModel[]> GetSourcesAsyncCore(CancellationToken cancellationToken = default)
        {
            var sources = await AppConfigurationLocalCache.GetValueAsync<AppConfigurationSourceModel[]>("sources", [], cancellationToken);
            if (sources != null && sources.Length > 0)
            {
                return sources;
            }
            return [new() { Uri = DefaultUris[0], Enabled = true },
                new() { Uri = DefaultUris[1], Enabled = true },];
        }

        /// <summary>
        /// 更新源列表
        /// </summary>
        /// <param name="sources"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<AppConfigurationSourceModel[]?> SetSourcesAsync(AppConfigurationSourceModel[]? sources, CancellationToken cancellationToken = default)
        {
            var list = new List<AppConfigurationSourceModel>();
            if (sources != null && sources.Length > 0)
            {
                var hash = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                for (int i = sources.Length - 1; i >= 0; i--)
                {
                    var uri = sources[i].Uri;
                    if (!string.IsNullOrEmpty(uri) && hash.Add(uri))
                    {
                        list.Add(sources[i]);
                    }
                }
            }

            await sourceLocker.WaitAsync(cancellationToken);

            try
            {
                sources = [.. list.Reverse<AppConfigurationSourceModel>()];
                var flag = await AppConfigurationLocalCache.SetValueAsync("sources", sources, cancellationToken);

                if (flag)
                {
                    return sources;
                }

                return null;
            }
            finally
            {
                sourceLocker.Release();
            }
        }

        public async Task<AppConfigurationSourceModel[]?> ResetSourcesAsync(CancellationToken cancellationToken = default)
        {
            return await SetSourcesAsync(null, cancellationToken).ConfigureAwait(false);
        }

        #endregion Sources Manager


        /// <summary>
        /// 请求最新配置
        /// </summary>
        /// <returns></returns>
        public async Task<bool> UpdateConfigurationAsync()
        {
            bool invokeEventFlag = false;
            bool success = false;

            var cancellationSource = Interlocked.CompareExchange(ref cancellationTokenSource, null, null);

            await updateConfigLocker.WaitAsync(cancellationSource?.Token ?? default);

            cancellationSource = new CancellationTokenSource(TimeSpan.FromSeconds(20));
            Interlocked.Exchange(ref cancellationTokenSource, cancellationSource);

            try
            {
                var client = HttpClientManager.CreateClient();

                var sources = await GetSourcesAsync(cancellationSource.Token);

                var enabledSources = sources
                    .Where(c => c.Enabled)
                    .Select(c =>
                    {
                        if (Uri.TryCreate(c.Uri, UriKind.Absolute, out var uri)
                            && uri.Scheme.ToLowerInvariant() switch
                            {
                                "http" => true,
                                "https" => true,
                                "file" => true,
                                _ => false
                            })
                        {
                            return uri;
                        }
                        return null;
                    })
                    .Where(c => c != null)
                    .OfType<Uri>()
                    .ToArray();

                if (enabledSources.Length > 0)
                {
                    var oldResult = await AppConfigurationLocalCache.GetValueAsync<string>("config", null, cancellationSource.Token);

                    var tasks = enabledSources.Select(c => Task.Run(async () =>
                    {
                        try
                        {
                            string? _json = null;
                            if (c.Scheme.Equals("file", StringComparison.OrdinalIgnoreCase))
                            {
                                if (System.IO.File.Exists(c.AbsolutePath))
                                {
                                    _json = await System.IO.File.ReadAllTextAsync(c.AbsolutePath, cancellationSource.Token);
                                }
                            }
                            else
                            {
                                _json = await client.GetStringAsync(c, cancellationSource.Token);
                            }

                            if (!string.IsNullOrEmpty(_json))
                            {
                                var _model = AppConfigurationModel.CreateFromJson(c.OriginalString, _json);
                                if (_model != null)
                                {
                                    return new AppConfigurationModelResult(
                                        _model,
                                        DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                                        c.OriginalString,
                                        _json);
                                }
                            }
                        }
                        catch { }

                        return null;
                    }, cancellationSource.Token));


                    var result = await TaskExtensions.WhenAny(tasks, c => c != null);

                    if (result != null)
                    {
                        await configLocker.WaitAsync(cancellationSource.Token);
                        try
                        {

                            var flag = (await AppConfigurationLocalCache.SetValueAsync("update", new AppConfigurationUpdateJsonModel()
                            {
                                Source = result.Source,
                                UpdateTime = result.UpdateTime
                            })) | (await AppConfigurationLocalCache.SetValueAsync("config", result.Json));

                            if (flag)
                            {
                                success = true;
                                invokeEventFlag = result.Json != oldResult;

                                currentConfiguration = result;
                            }
                        }
                        finally
                        {
                            configLocker.Release();
                        }
                    }
                }
            }
            catch { }
            finally
            {
                cancellationTokenSource = null;
                updateConfigLocker.Release();
            }

            if (invokeEventFlag)
            {
                ConfigurationChanged?.Invoke(this, EventArgs.Empty);
            }

            return success;
        }

        public bool CancelUpdate()
        {
            var source = Interlocked.Exchange(ref cancellationTokenSource, null);
            source?.Cancel();
            return source != null;
        }

        /// <summary>
        /// 移除本地缓存的配置
        /// </summary>
        /// <returns></returns>
        public async Task<bool> RemoveLocalCacheAsync()
        {
            await configLocker.WaitAsync();

            try
            {
                var flag = (await AppConfigurationLocalCache.SetValueAsync<string>("update", null))
                    | (await AppConfigurationLocalCache.SetValueAsync<string>("config", null));

                if (flag)
                {
                    currentConfiguration = null;
                }
                return flag;
            }
            finally
            {
                configLocker.Release();
            }
        }

        /// <summary>
        /// 获取当前配置，如果有缓存则读取缓存的，没有缓存则读取安装目录的
        /// </summary>
        /// <returns></returns>
        public async Task<AppConfigurationModelResult> GetLocalConfigurationAsync(CancellationToken cancellationToken = default)
        {
            if (currentConfiguration != null)
                return currentConfiguration;

            await configLocker.WaitAsync(cancellationToken);

            try
            {
                var updateModel = await AppConfigurationLocalCache.GetValueAsync<AppConfigurationUpdateJsonModel>("update", null, cancellationToken);
                if (updateModel != null)
                {
                    var json = await AppConfigurationLocalCache.GetValueAsync<string>("config", null, cancellationToken);

                    if (!string.IsNullOrEmpty(json))
                    {
                        var model = AppConfigurationModel.CreateFromJson(updateModel.Source, json);
                        if (model != null)
                        {
                            return (currentConfiguration = new AppConfigurationModelResult(model, updateModel.UpdateTime, updateModel.Source, json));
                        }
                    }
                }

                {
                    var presetJsonFilePath = System.IO.Path.Combine(AppContext.BaseDirectory, "configuration.json");
                    var json = await System.IO.File.ReadAllTextAsync(presetJsonFilePath, cancellationToken);
                    var model = AppConfigurationModel.CreateFromJson(presetJsonFilePath, json);

                    ArgumentNullException.ThrowIfNull(model);

                    return (currentConfiguration = new AppConfigurationModelResult(model, 0, "", ""));
                }
            }
            finally
            {
                configLocker.Release();
            }
        }

        public event EventHandler? ConfigurationChanged;


        private static class AppConfigurationLocalCache
        {
            public static async Task<T?> GetValueAsync<T>(string key, T? defaultValue, CancellationToken cancellationToken = default)
            {
                var folder = System.IO.Path.Combine(ApplicationData.Current.LocalCacheFolder.Path, "config");

                if (!key.AsSpan().EndsWith(".json")) key = $"{key}.json";

                var filePath = System.IO.Path.Combine(folder, key);

                return await Task.Run(async () =>
                {
                    if (System.IO.Path.Exists(filePath))
                    {
                        var json = await System.IO.File.ReadAllTextAsync(filePath, cancellationToken);
                        if (typeof(T) == typeof(string))
                        {
                            return (T?)(object?)(json) ?? defaultValue;
                        }
                        try
                        {
                            return JsonConvert.DeserializeObject<T>(json) ?? defaultValue;
                        }
                        catch { }
                    }
                    return defaultValue;
                }, cancellationToken);
            }

            public static async Task<bool> SetValueAsync<T>(string key, T? value, CancellationToken cancellationToken = default)
            {
                var folder = System.IO.Path.Combine(ApplicationData.Current.LocalCacheFolder.Path, "config");

                if (!key.AsSpan().EndsWith(".json")) key = $"{key}.json";

                var filePath = System.IO.Path.Combine(folder, key);

                return await Task.Run(async () =>
                {
                    if (value is null || value is "")
                    {
                        if (System.IO.Path.Exists(filePath))
                        {
                            try { System.IO.File.Delete(filePath); return true; }
                            catch { }
                            return false;
                        }
                    }
                    else
                    {
                        var json = "";
                        if (typeof(T) == typeof(string))
                        {
                            json = (string)(object)value;
                        }
                        else
                        {
                            json = JsonConvert.SerializeObject(value);
                        }

                        try
                        {
                            if (!System.IO.Directory.Exists(folder))
                            {
                                System.IO.Directory.CreateDirectory(folder);
                            }
                            await System.IO.File.WriteAllTextAsync(filePath, json, Encoding.UTF8, cancellationToken);
                            return true;
                        }
                        catch { }
                    }

                    return false;
                }, cancellationToken);
            }
        }

        private static class TaskExtensions
        {
            public static async Task<T?> WhenAny<T>(IEnumerable<Task<T?>> tasks, Func<T?, bool> predicate)
            {
                TaskCompletionSource<T?>? tcs = null;
                var wrappers = tasks.Select(async task =>
                {
                    try
                    {
                        var c = await task;
                        if (predicate(c))
                        {
                            tcs?.TrySetResult(c);
                        }
                    }
                    catch { }
                }).ToArray();

                if (wrappers.Length > 0)
                {
                    tcs = new TaskCompletionSource<T?>();

                    _ = new Func<Task>(async () =>
                    {
                        await Task.WhenAll(wrappers);
                        tcs.TrySetResult(default);
                    }).Invoke();

                    return await tcs.Task;
                }

                return default;
            }
        }

        private class AppConfigurationUpdateJsonModel
        {
            public long UpdateTime { get; set; }

            public string Source { get; set; } = "";
        }

        public class AppConfigurationSourceModel : ObservableObject
        {
            public string? Uri { get; set; }

            public bool Enabled { get; set; }
        }

        public record class AppConfigurationModelResult(AppConfigurationModel AppConfiguration, long UpdateTime, string Source, string Json);
    }
}
