using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using HotLyric.Win32.Utils.LrcProviders;
using Kfstorm.LrcParser;
using Newtonsoft.Json.Linq;
using Windows.Storage;

namespace HotLyric.Win32.Utils
{
    internal static class LrcHelper
    {
        static LrcHelper()
        {
            Providers = new Dictionary<string, ILrcProvider>()
            {
                ["NetEase"] = new NetEaseLrcProvider(),
                ["QQMusic"] = new QQMusicLrcProvider()
            };
        }

        private static IReadOnlyDictionary<string, ILrcProvider> Providers { get; }
        private static readonly HashSet<string> absoluteMusicLyricFlags = new HashSet<string>()
        {
            "纯音乐，请欣赏",
            "此歌曲为没有填词的纯音乐，请您欣赏"
        };

        public static readonly ILrcFile EmptyLyric = LrcFile.FromText("[00:00.00]暂无歌词");
        public static readonly ILrcFile DownloadingLyric = LrcFile.FromText("[00:00.00]正在加载...");

        /// <summary>
        /// 读取播放器传递的本地歌词
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public static async Task<LyricModel?> GetLrcFileAsync(string filePath, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(filePath)) return null;

            try
            {
                if (!System.IO.File.Exists(filePath)) return null;
                var content = await System.IO.File.ReadAllTextAsync(filePath, cancellationToken);

                var lrcFile = LrcFile.FromText(content);

                if (lrcFile?.Lyrics?.Any(c => absoluteMusicLyricFlags.Contains(c.Content)) == true)
                {
                    return null;
                }

                return new LyricModel(lrcFile, null, content, null);
            }
            catch (Exception ex) when (!(ex is OperationCanceledException)) { }
            return null;
        }

        /// <summary>
        /// 获取缓存或网络歌词
        /// </summary>
        /// <param name="name"></param>
        /// <param name="artist"></param>
        /// <param name="neteaseMusicId"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public static async Task<LyricModel?> GetLrcFileAsync(string? name, string[]? artists, string neteaseMusicId, string? defaultProviderName, bool convertToSimpleChinese, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(name)) return null;

            var file = await LrcProviderHelper.GetLyricFromCacheAsync(name, artists, cancellationToken);
            if (file == null) file = await GetLyricFromNetworkAsync(name, artists, neteaseMusicId, defaultProviderName, convertToSimpleChinese, cancellationToken);

            // 如果全是空行则认为歌词不存在
            if (file?.Lyric != null && file.Lyric.Lyrics.All(c => string.IsNullOrWhiteSpace(c.Content)))
            {
                return null;
            }

            if (file?.Lyric?.Lyrics?.Any(c => absoluteMusicLyricFlags.Contains(c.Content)) == true)
            {
                return null;
            }

            return file;
        }

        /// <summary>
        /// 获取网络歌词
        /// </summary>
        /// <param name="name"></param>
        /// <param name="artist"></param>
        /// <param name="neteaseMusicId"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private static async Task<LyricModel?> GetLyricFromNetworkAsync(string? name, string[]? artists, string? neteaseMusicId, string? defaultProviderName, bool convertToSimpleChinese, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(name)) return null;

            try
            {
                if (neteaseMusicId?.StartsWith("ncm-", StringComparison.OrdinalIgnoreCase) == true)
                {
                    neteaseMusicId = neteaseMusicId.Substring(4);
                }

                var lyric = await Providers["NetEase"].GetByIdAsync(neteaseMusicId!, cancellationToken);
                if (lyric != null)
                {
                    await LrcProviderHelper.SetLyricCacheAsync(LrcProviderHelper.BuildSearchKey(name, artists), lyric.LyricContent, lyric.TranslateContent, cancellationToken);

                    if (lyric?.Lyric?.Lyrics?.Any(c => absoluteMusicLyricFlags.Contains(c.Content)) == true)
                    {
                        return null;
                    }

                    return lyric;
                }

                List<ILrcProvider> providers = new List<ILrcProvider>();

                if (!string.IsNullOrEmpty(defaultProviderName) && Providers.TryGetValue(defaultProviderName, out var defaultProvider))
                {
                    providers.Add(defaultProvider);
                }
                else
                {
                    defaultProvider = null;
                }

                foreach (var item in Providers)
                {
                    if (item.Key != defaultProvider?.Name)
                    {
                        providers.Add(item.Value);
                    }
                }

                var (_name, _artists) = LrcProviderHelper.ConvertNameAndArtists(name, artists, convertToSimpleChinese);

                if (string.IsNullOrEmpty(_name)) return null;

                foreach (var provider in providers)
                {
                    var id = await provider.GetIdAsync(_name, _artists, cancellationToken);
                    if (id != null)
                    {
                        lyric = await provider.GetByIdAsync(id, cancellationToken);

                        if (lyric != null)
                        {
                            await LrcProviderHelper.SetLyricCacheAsync(LrcProviderHelper.BuildSearchKey(name, artists), lyric.LyricContent, lyric.TranslateContent, cancellationToken);
                            
                            if (lyric?.Lyric?.Lyrics?.Any(c => absoluteMusicLyricFlags.Contains(c.Content)) == true)
                            {
                                return null;
                            }

                            return lyric;
                        }
                    }
                }
            }
            catch (Exception ex) when (!(ex is OperationCanceledException)) { }

            return null;
        }

        public static async Task ClearCacheAsync()
        {
            var folder = await ApplicationData.Current.TemporaryFolder.CreateFolderAsync("cache", CreationCollisionOption.OpenIfExists);
            if (folder != null)
            {
                foreach (var item in await folder.GetItemsAsync())
                {
                    try
                    {
                        await item.DeleteAsync(StorageDeleteOption.PermanentDelete);
                    }
                    catch { }
                }
            }
        }

    }

    public class LyricModel
    {
        public LyricModel(ILrcFile? lyric, ILrcFile? translated, string? lyricContent, string? translateContent)
        {
            Lyric = lyric;
            Translated = translated;
            LyricContent = lyricContent;
            TranslateContent = translateContent;
        }

        public ILrcFile? Lyric { get; }

        public ILrcFile? Translated { get; }

        public string? LyricContent { get; }

        public string? TranslateContent { get; }
    }
}
