using HotLyric.Win32.Models.AppConfigurationModels;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;

namespace HotLyric.Win32.Utils.AppConfigurations
{
    public class AppConfigurationManager
    {
        private SemaphoreSlim locker = new SemaphoreSlim(1, 1);

        private CancellationTokenSource? cancellationTokenSource;
        private AppConfigurationModel? currentConfiguration;

        /// <summary>
        /// 请求最新配置
        /// </summary>
        /// <returns></returns>
        public async Task<bool> UpdateConfigurationAsync(string updateSource)
        {
            bool invokeEventFlag = false;

            var source = Interlocked.CompareExchange(ref cancellationTokenSource, null, null);

            await locker.WaitAsync(source?.Token ?? default);

            source = new CancellationTokenSource();
            Interlocked.Exchange(ref cancellationTokenSource, source);

            try
            {
                var client = HttpClientManager.CreateClient();
                var json = await client.GetStringAsync(updateSource, source.Token);

                if (json != null)
                {
                    var model = AppConfigurationModel.CreateFromJson(updateSource, json);
                    if (model != null)
                    {
                        var updateModel = new AppConfigurationUpdateJsonModel()
                        {
                            UpdateTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                            Source = updateSource
                        };

                        var oldResult = await AppConfigurationLocalCache.GetCacheAsync(source.Token);

                        var flag = await AppConfigurationLocalCache.SetCacheAsync(updateModel, json);
                        if (flag)
                        {
                            currentConfiguration = model;
                        }

                        invokeEventFlag = flag && json != oldResult?.AppConfigurationJson;
                    }
                }
            }
            catch { }
            finally
            {
                cancellationTokenSource = null;
                locker.Release();
            }

            if (invokeEventFlag)
            {
                ConfigurationChanged?.Invoke(this, EventArgs.Empty);
            }

            return false;
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
            await locker.WaitAsync();

            try
            {
                var flag = await AppConfigurationLocalCache.ClearCacheAsync();
                if (flag)
                {
                    currentConfiguration = null;
                }
                return flag;
            }
            finally
            {
                locker.Release();
            }
        }

        /// <summary>
        /// 获取当前配置，如果有缓存则读取缓存的，没有缓存则读取安装目录的
        /// </summary>
        /// <returns></returns>
        public async Task<AppConfigurationModel> GetLocalConfigurationAsync(CancellationToken cancellationToken = default)
        {
            if (currentConfiguration != null)
                return currentConfiguration;

            AppConfigurationResult? result;

            await locker.WaitAsync(cancellationToken);

            try
            {
                result = await AppConfigurationLocalCache.GetCacheAsync(cancellationToken);

                if (result != null)
                {
                    var model = AppConfigurationModel.CreateFromJson(result.UpdateInfo.Source, result.AppConfigurationJson);
                    if (model != null)
                    {
                        currentConfiguration = model;
                        return model;
                    }
                }

                {
                    var presetJsonFilePath = System.IO.Path.Combine(AppContext.BaseDirectory, "configuration.json");
                    var json = await System.IO.File.ReadAllTextAsync(presetJsonFilePath, cancellationToken);
                    var model = AppConfigurationModel.CreateFromJson(presetJsonFilePath, json);

                    currentConfiguration = model;
                    return model!;
                }
            }
            finally
            {
                locker.Release();
            }
        }

        public event EventHandler? ConfigurationChanged;

        private static class AppConfigurationLocalCache
        {
            public static async Task<bool> ClearCacheAsync()
            {
                var cacheConfigFolder = System.IO.Path.Combine(ApplicationData.Current.LocalCacheFolder.Path, "config");
                return await Task.Run(() =>
                {
                    try
                    {
                        if (System.IO.Directory.Exists(cacheConfigFolder))
                        {
                            System.IO.Directory.Delete(cacheConfigFolder, true);
                        }
                        return true;
                    }
                    catch { }
                    return false;
                });
            }

            public static async Task<bool> SetCacheAsync(AppConfigurationUpdateJsonModel updateInfo, string config)
            {
                try
                {
                    var cacheConfigFolder = await ApplicationData.Current.LocalCacheFolder.CreateFolderAsync("config", CreationCollisionOption.OpenIfExists);

                    await Task.WhenAll(
                        Task.Run(async () =>
                        {
                            var updateInfoFile = await cacheConfigFolder.CreateFileAsync("update.json", CreationCollisionOption.ReplaceExisting);
                            await FileIO.WriteTextAsync(updateInfoFile, JsonConvert.SerializeObject(updateInfo));
                        }),
                        Task.Run(async () =>
                        {
                            var configFile = await cacheConfigFolder.CreateFileAsync("config.json", CreationCollisionOption.ReplaceExisting);
                            await FileIO.WriteTextAsync(configFile, config);
                        }));

                    return true;
                }
                catch { }

                return false;
            }

            public static async Task<AppConfigurationResult?> GetCacheAsync(CancellationToken cancellationToken = default)
            {
                var cacheConfigFolder = System.IO.Path.Combine(ApplicationData.Current.LocalCacheFolder.Path, "config");
                if (System.IO.Directory.Exists(cacheConfigFolder))
                {
                    var updateInfoFile = System.IO.Path.Combine(cacheConfigFolder, "update.json");
                    var configFile = System.IO.Path.Combine(cacheConfigFolder, "config.json");

                    try
                    {
                        string updateInfo = "";
                        string config = "";

                        await Task.WhenAll(
                            Task.Run(async () =>
                            {
                                if (System.IO.Path.Exists(updateInfoFile)) { updateInfo = await System.IO.File.ReadAllTextAsync(updateInfoFile, cancellationToken); }
                            }),
                            Task.Run(async () =>
                            {
                                if (System.IO.Path.Exists(configFile)) { config = await System.IO.File.ReadAllTextAsync(configFile, cancellationToken); }
                            }));

                        if (!string.IsNullOrEmpty(updateInfo) && !string.IsNullOrEmpty(config))
                        {
                            try
                            {
                                var updateModel = JsonConvert.DeserializeObject<AppConfigurationUpdateJsonModel>(updateInfo);
                                if (updateModel != null)
                                {
                                    return new AppConfigurationResult(updateModel, config);
                                }
                            }
                            catch { }
                        }
                    }
                    catch { }
                }

                return default;
            }
        }

        private class AppConfigurationUpdateJsonModel
        {
            public long UpdateTime { get; set; }

            public string Source { get; set; } = "";
        }

        private record class AppConfigurationResult(
            AppConfigurationUpdateJsonModel UpdateInfo,
            string AppConfigurationJson);
    }
}
