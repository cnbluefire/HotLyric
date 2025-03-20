using HotLyric.Win32.Utils.LyricFiles;
using Lyricify.Lyrics.Helpers.General;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;

namespace HotLyric.Win32.Utils.LrcProviders
{
    internal static partial class LrcProviderHelper
    {
        [GeneratedRegex("(-|\\(|\\)|/|\\\\|&)")]
        private static partial Regex GetReplaceRegex();

        [GeneratedRegex("(\\s{2,})")]
        private static partial Regex GetReplaceSpaceRegex();

        /// <summary>
        /// 英文名转为中文名，繁体转为简体
        /// </summary>
        /// <param name="name"></param>
        /// <param name="artists"></param>
        /// <param name="convertToSimpleChinese"></param>
        /// <returns></returns>
        public static (string? name, string? artists) ConvertNameAndArtists(string? name, string? artists, bool convertToSimpleChinese)
        {
            artists ??= string.Empty;

            foreach (var (id, k, v) in Lyricify.Lyrics.Searchers.Helpers.ArtistHelper.ArtistNamePairs)
            {
                artists = artists.Replace(k, v);
            }

            artists = ChineseConverter.ConvertToSimplifiedChinese(artists);

            if (convertToSimpleChinese && !string.IsNullOrEmpty(name))
            {
                try
                {
                    name = ChineseConverter.ConvertToSimplifiedChinese(name);
                }
                catch (Exception ex)
                {
                    HotLyric.Win32.Utils.LogHelper.LogError(ex);
                }
            }

            return (name, artists);
        }

        /// <summary>
        /// 缓存歌词
        /// </summary>
        /// <param name="searchKey"></param>
        /// <param name="lrcContent"></param>
        /// <param name="translatedContent"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        internal static async Task SetLyricCacheAsync(string? searchKey, string? lrcContent, string? translatedContent, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(searchKey)) return;
            if (string.IsNullOrEmpty(lrcContent)) return;

            var md5 = GetMD5(searchKey);

            try
            {
                var folder = await ApplicationData.Current.TemporaryFolder.CreateFolderAsync("cache", CreationCollisionOption.OpenIfExists).AsTask(cancellationToken);
                if (folder != null)
                {
                    if ((await folder.TryGetItemAsync(md5).AsTask(cancellationToken)) is StorageFile file)
                    {
                        await file.DeleteAsync(StorageDeleteOption.PermanentDelete).AsTask(cancellationToken);
                    }

                    if ((await folder.TryGetItemAsync($"{md5}_trans").AsTask(cancellationToken)) is StorageFile file2)
                    {
                        await file2.DeleteAsync(StorageDeleteOption.PermanentDelete).AsTask(cancellationToken);
                    }

                    file = await folder.CreateFileAsync(md5).AsTask(cancellationToken);
                    await FileIO.WriteTextAsync(file, lrcContent).AsTask(cancellationToken);

                    file2 = await folder.CreateFileAsync($"{md5}_trans").AsTask(cancellationToken);
                    await FileIO.WriteTextAsync(file2, translatedContent).AsTask(cancellationToken);
                }
            }
            catch (Exception ex) when (!(ex is OperationCanceledException))
            {
                HotLyric.Win32.Utils.LogHelper.LogError(ex);
            }
        }

        /// <summary>
        /// 获取缓存的歌词
        /// </summary>
        /// <param name="name"></param>
        /// <param name="artist"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        internal static async Task<Lyric?> GetLyricFromCacheAsync(string? name, string? artists, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(name)) return null;

            var md5 = GetMD5(BuildSearchKey(name, artists));

            try
            {
                var folder = await ApplicationData.Current.TemporaryFolder.CreateFolderAsync("cache", CreationCollisionOption.OpenIfExists).AsTask(cancellationToken);
                if (folder != null && (await folder.TryGetItemAsync(md5).AsTask(cancellationToken)) is StorageFile file)
                {
                    var text = await FileIO.ReadTextAsync(file).AsTask(cancellationToken);
                    string? text2 = null;

                    if (await folder.TryGetItemAsync($"{md5}_trans").AsTask(cancellationToken) is StorageFile file2)
                    {
                        try
                        {
                            text2 = await FileIO.ReadTextAsync(file2).AsTask(cancellationToken);
                        }
                        catch (Exception ex) when (!(ex is OperationCanceledException))
                        {
                            HotLyric.Win32.Utils.LogHelper.LogError(ex);
                        }
                    }

                    return Lyric.CreateClassicLyric(text, text2, name, artists);
                }
            }
            catch (Exception ex) when (!(ex is OperationCanceledException))
            {
                HotLyric.Win32.Utils.LogHelper.LogError(ex);
            }

            return null;
        }


        public static async Task<string> TryGetStringAsync(string uri, CancellationToken cancellationToken)
        {
            return await TryGetStringAsync(uri, "", cancellationToken);
        }

        public static async Task<string> TryGetStringAsync(string uri, string referer, CancellationToken cancellationToken)
        {
            var client = HttpClientManager.CreateClient();

            try
            {
                var req = new HttpRequestMessage(HttpMethod.Get, uri);
                if (!string.IsNullOrEmpty(referer) && Uri.TryCreate(referer, UriKind.Absolute, out var refererUrl))
                {
                    req.Headers.Referrer = refererUrl;
                }

                var resp = await client.SendAsync(req, cancellationToken);
                return await resp.Content.ReadAsStringAsync();
            }
            catch (Exception ex) when (!(ex is OperationCanceledException))
            {
                HotLyric.Win32.Utils.LogHelper.LogError(ex);
            }

            return String.Empty;
        }


        internal static string GetMD5(string str)
        {
            using (var md5 = MD5.Create())
            {
                return md5.ComputeHash(Encoding.UTF8.GetBytes(str ?? ""))
                    .Aggregate(new StringBuilder(), (s, c) => s.AppendFormat("{0:x2}", c))
                    .ToString();
            }
        }

        internal static string? CombineArtists(string[]? artists)
        {
            return artists != null ? string.Join(" ", artists.Where(c => !string.IsNullOrEmpty(c))) : null;
        }

        internal static string BuildSearchKey(string? name, string[]? artists)
        {
            return BuildSearchKey(name, CombineArtists(artists));
        }


        internal static string BuildSearchKey(string? name, string? artist)
        {
            var searchKey = string.Empty;

            if (string.IsNullOrEmpty(artist))
            {
                searchKey = name ?? string.Empty;
            }
            else
            {
                searchKey = $"{name} {artist}";
            }

            return searchKey;
        }

        internal static string GetSearchKey(string? str)
        {
            if (string.IsNullOrEmpty(str)) return string.Empty;

            str = GetReplaceRegex().Replace(str, "");
            str = GetReplaceSpaceRegex().Replace(str, " ");

            return str;
        }

        /// <summary>
        /// 获取与Key最相似的歌曲信息
        /// </summary>
        /// <param name="key"></param>
        /// <param name="musicInfos"></param>
        /// <param name="judgeLength"></param>
        /// <returns></returns>
        internal static MusicInfomation? GetMostSimilarMusicInfomation(string? key, MusicInfomation[] musicInfos, int judgeLength)
        {
            if (string.IsNullOrEmpty(key) || musicInfos == null || musicInfos.Length == 0) return null;

            var lev = new Fastenshtein.Levenshtein(key);
            MusicInfomation? curInfo = null;
            int minLen = int.MaxValue;

            foreach (var info in musicInfos)
            {
                if (!string.IsNullOrEmpty(info.Name))
                {
                    var sb = new StringBuilder(info.Name);
                    if (info.Artists != null)
                    {
                        foreach (var _artist in info.Artists)
                        {
                            if (!string.IsNullOrEmpty(_artist))
                            {
                                sb.Append(' ')
                                    .Append(_artist);

                                var _tmpKey = sb.ToString();
                                if (!string.IsNullOrWhiteSpace(_tmpKey))
                                {
                                    var len = lev.DistanceFrom(GetSearchKey(_tmpKey));
                                    if (len < minLen)
                                    {
                                        minLen = len;
                                        curInfo = info;
                                    }
                                }
                            }
                        }
                    }

                    var tmpKey = sb.ToString();
                    if (!string.IsNullOrWhiteSpace(tmpKey))
                    {
                        var len = lev.DistanceFrom(GetSearchKey(tmpKey));
                        if (len < minLen)
                        {
                            minLen = len;
                            curInfo = info;
                        }
                    }
                }
            }

            return minLen <= judgeLength ? curInfo : null;
        }

        internal class MusicInfomation
        {
            public MusicInfomation(string? id, string? name, string[] artists)
            {
                Id = id;
                Name = name;
                Artists = artists;
            }

            public string? Id { get; set; }

            public string? Name { get; set; }

            public string[] Artists { get; set; }
        }
    }
}
