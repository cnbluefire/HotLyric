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
using Kfstorm.LrcParser;
using Newtonsoft.Json.Linq;
using Windows.Storage;

namespace HotLyric.Win32.Utils
{
    internal static class LrcHelper
    {
        private static HttpClient? client;

        private static Regex replaceRegex = new Regex("(-|\\(|\\)|/|\\\\|&)");
        private static Regex replaceSpaceRegex = new Regex("(\\s{2,})");

        public static readonly ILrcFile EmptyLyric = LrcFile.FromText("[00:00.00]暂无歌词");
        public static readonly ILrcFile DownloadingLyric = LrcFile.FromText("[00:00.00]正在加载...");

        /// <summary>
        /// 读取播放器传递的本地歌词
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public static async Task<NetEaseLyric?> GetLrcFileAsync(string filePath, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(filePath)) return null;

            try
            {
                if (!System.IO.File.Exists(filePath)) return null;
                var content = await System.IO.File.ReadAllTextAsync(filePath, cancellationToken);
                var lrcFile = LrcFile.FromText(content);
                return new NetEaseLyric(lrcFile, null);
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
        public static async Task<NetEaseLyric?> GetLrcFileAsync(string? name, string? artist, string neteaseMusicId, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(name)) return null;

            var file = await GetLyricFromCacheAsync(name, artist, cancellationToken);
            if (file == null) file = await GetLyricFromNetworkAsync(name, artist, neteaseMusicId, cancellationToken);

            // 如果全是空行则认为歌词不存在
            if (file?.Lyric != null && file.Lyric.Lyrics.All(c => string.IsNullOrWhiteSpace(c.Content)))
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
        private static async Task<NetEaseLyric?> GetLyricFromNetworkAsync(string? name, string? artist, string? neteaseMusicId, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(name)) return null;

            try
            {
                if (string.IsNullOrEmpty(neteaseMusicId))
                {
                    neteaseMusicId = await GetNeteaseMusicIdAsync(name, artist, cancellationToken);
                }
                else if (neteaseMusicId.StartsWith("ncm-", StringComparison.OrdinalIgnoreCase))
                {
                    neteaseMusicId = neteaseMusicId.Substring(4);
                }

                if (!string.IsNullOrEmpty(neteaseMusicId))
                {
                    return await GetNeteaseMusicLyricAsync(neteaseMusicId, BuildSearchKey(name, artist), cancellationToken);
                }

                var qqSongMid = await GetQQMusicIdAsync(name, artist, cancellationToken);
                if (!string.IsNullOrEmpty(qqSongMid))
                {
                    return await GetQQMusicLyricAsync(qqSongMid, "", cancellationToken);
                }
            }
            catch (Exception ex) when (!(ex is OperationCanceledException)) { }

            return null;
        }

        #region 缓存

        private static async Task SetLyricCacheAsync(string? searchKey, string? lrcContent, string? translatedContent, CancellationToken cancellationToken)
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
            catch (Exception ex) when (!(ex is OperationCanceledException)) { }
        }

        private static async Task<NetEaseLyric?> GetLyricFromCacheAsync(string? name, string? artist, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(name)) return null;

            var md5 = GetMD5(BuildSearchKey(name, artist));

            try
            {
                var folder = await ApplicationData.Current.TemporaryFolder.CreateFolderAsync("cache", CreationCollisionOption.OpenIfExists).AsTask(cancellationToken);
                if (folder != null && (await folder.TryGetItemAsync(md5).AsTask(cancellationToken)) is StorageFile file)
                {
                    var text = await FileIO.ReadTextAsync(file).AsTask(cancellationToken);

                    var lrcFile = LrcFile.FromText(text);

                    ILrcFile? translated = null;

                    if (await folder.TryGetItemAsync($"{md5}_trans").AsTask(cancellationToken) is StorageFile file2)
                    {
                        try
                        {
                            var text2 = await FileIO.ReadTextAsync(file2).AsTask(cancellationToken);
                            if (!string.IsNullOrEmpty(text2))
                            {
                                translated = LrcFile.FromText(text2);

                                if (translated.Lyrics.All(c => string.IsNullOrEmpty(c.Content)))
                                {
                                    translated = null;
                                }
                            }
                        }
                        catch (Exception ex) when (!(ex is OperationCanceledException)) { }
                    }

                    return new NetEaseLyric(lrcFile, translated);
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

        #endregion 缓存


        #region 网易云音乐api

        private static async Task<string?> GetNeteaseMusicIdAsync(string? name, string? artist, CancellationToken cancellationToken)
        {
            try
            {
                var searchKeyword = BuildSearchKey(name, artist);
                var key = GetSearchKey(searchKeyword);
                if (string.IsNullOrEmpty(key)) return null;

                var json = await TryGetStringAsync($"http://music.163.com/api/search/get/web?csrf_token=hlpretag=&hlposttag=&s={Uri.EscapeDataString(searchKeyword)}&type=1&offset=0&total=true&limit=10", cancellationToken);
                if (string.IsNullOrEmpty(json)) return null;

                var jobj = JObject.Parse(json);
                var arr = jobj?["result"]?["songs"] as JArray;

                if (arr?.Count > 0)
                {
                    Fastenshtein.Levenshtein lev = new Fastenshtein.Levenshtein(key);
                    var minLen = int.MaxValue;
                    string? id = "";

                    foreach (var item in arr)
                    {
                        var _id = (string?)item["id"];

                        var sb = new StringBuilder((string?)item["name"] ?? "");
                        var artists = item.Value<JArray>("artists");
                        if (artists != null)
                        {
                            foreach (var _artist in artists)
                            {
                                sb.Append(' ')
                                    .Append((string?)_artist["name"] ?? "");
                            }
                        }

                        var tmpKey = sb.ToString();
                        if (!string.IsNullOrWhiteSpace(tmpKey))
                        {
                            var len = lev.DistanceFrom(GetSearchKey(tmpKey));
                            if (len < minLen)
                            {
                                minLen = len;
                                id = _id;
                            }
                        }
                    }

                    Debug.WriteLine($"GetNeteaseMusicIdAsync: minLen: {minLen}, maxLen: {(int)Math.Ceiling(key.Length / 6d)}");
                    return minLen <= (int)Math.Ceiling(key.Length / 6d) ? id : null;
                }
            }
            catch (Exception ex) when (!(ex is OperationCanceledException)) { }

            return null;
        }

        private static async Task<NetEaseLyric?> GetNeteaseMusicLyricAsync(string id, string cacheKey, CancellationToken cancellationToken)
        {
            if (!string.IsNullOrEmpty(id))
            {
                try
                {
                    var json = await TryGetStringAsync($"https://music.163.com/api/song/lyric?id={id}&lv=-1&kv=1&tv=-1", cancellationToken);
                    if (string.IsNullOrEmpty(json)) return null;

                    var jobj = JObject.Parse(json);
                    var lrcContent = (string?)jobj?["lrc"]?["lyric"];

                    if (string.IsNullOrEmpty(lrcContent)) return null;
                    var lrcFile = LrcFile.FromText(lrcContent);

                    if (lrcFile != null)
                    {
                        string? translatedContent = "";
                        ILrcFile? translated = null;
                        try
                        {
                            translatedContent = (string?)jobj?["tlyric"]?["lyric"];
                            if (!string.IsNullOrEmpty(translatedContent))
                            {
                                translated = LrcFile.FromText(translatedContent);
                                if (translated.Lyrics.All(c => string.IsNullOrEmpty(c.Content)))
                                {
                                    translated = null;
                                    translatedContent = "";
                                }
                            }
                        }
                        catch { }

                        await SetLyricCacheAsync(cacheKey, lrcContent, translatedContent, cancellationToken);

                        return new NetEaseLyric(lrcFile, translated);
                    }
                }
                catch (Exception ex) when (!(ex is OperationCanceledException)) { }
            }
            return null;
        }

        #endregion 网易云音乐api


        #region QQ音乐api

        private static async Task<string?> GetQQMusicIdAsync(string? name, string? artist, CancellationToken cancellationToken)
        {
            const int pageSize = 20;

            try
            {
                var searchKey = BuildSearchKey(name, artist);
                var key = GetSearchKey(searchKey);

                if (string.IsNullOrEmpty(key)) return null;

                var json = await TryGetStringAsync($"http://c.y.qq.com/soso/fcgi-bin/client_search_cp?format=json&n={pageSize}&p=1&w={Uri.EscapeDataString(key)}&cr=1&g_tk=5381&t=0", "https://y.qq.com", cancellationToken);

                if (string.IsNullOrEmpty(json)) return null;

                var jObj = JObject.Parse(json);
                if (jObj != null
                    && jObj.ContainsKey("code")
                    && jObj?["code"]?.Type == JTokenType.Integer
                    && jObj.Value<int>("code") == 0)
                {
                    var arr = jObj["data"]?["song"]?["list"] as JArray;
                    if (arr != null && arr.Count > 0)
                    {
                        Fastenshtein.Levenshtein lev = new Fastenshtein.Levenshtein(key);
                        var minLen = int.MaxValue;
                        string? id = "";

                        foreach (var item in arr)
                        {
                            var _id = (string?)item["songmid"];

                            var sb = new StringBuilder((string?)item["songname"] ?? "");
                            var artists = item.Value<JArray>("singer");
                            if (artists != null)
                            {
                                foreach (var _artist in artists)
                                {
                                    sb.Append(' ')
                                        .Append((string?)_artist["name"] ?? "");
                                }
                            }

                            var tmpKey = sb.ToString();
                            if (!string.IsNullOrWhiteSpace(tmpKey))
                            {
                                var len = lev.DistanceFrom(GetSearchKey(tmpKey));
                                if (len < minLen)
                                {
                                    minLen = len;
                                    id = _id;
                                }
                            }
                        }

                        Debug.WriteLine($"GetQQMusicIdAsync: minLen: {minLen}, maxLen: {(int)Math.Ceiling(key.Length / 6d)}");
                        return minLen <= (int)Math.Ceiling(key.Length / 6d) ? id : null;

                    }
                }
            }
            catch (Exception ex) when (!(ex is OperationCanceledException)) { }

            return null;
        }


        private static async Task<NetEaseLyric?> GetQQMusicLyricAsync(string id, string cacheKey, CancellationToken cancellationToken)
        {
            if (!string.IsNullOrEmpty(id))
            {
                try
                {
                    var query = new Dictionary<string, string>()
                    {
                        ["songmid"] = id,
                        ["pcachetime"] = $"{DateTimeOffset.Now.ToUnixTimeMilliseconds()}",
                        ["g_tk"] = "5381",
                        ["loginUin"] = "0",
                        ["hostUin"] = "0",
                        ["inCharset"] = "utf8",
                        ["outCharset"] = "utf-8",
                        ["notice"] = "0",
                        ["platform"] = "yqq",
                        ["needNewCode"] = "0",
                        ["format"] = "json",
                    };

                    var queryStr = string.Join("&", query.Select(c => $"{Uri.EscapeDataString(c.Key)}={Uri.EscapeDataString(c.Value)}"));

                    var json = await TryGetStringAsync($"http://c.y.qq.com/lyric/fcgi-bin/fcg_query_lyric_new.fcg?{queryStr}", "https://y.qq.com/portal/player.html", cancellationToken);
                    if (string.IsNullOrEmpty(json)) return null;

                    var jobj = JObject.Parse(json);
                    var lrcBase64 = (string?)jobj?["lyric"];
                    if (string.IsNullOrEmpty(lrcBase64)) return null;

                    var lrcContent = "";
                    try
                    {
                        lrcContent = Encoding.UTF8.GetString(Convert.FromBase64String(lrcBase64));
                    }
                    catch { }

                    if (string.IsNullOrEmpty(lrcContent)) return null;
                    var lrcFile = LrcFile.FromText(lrcContent);

                    if (lrcFile != null)
                    {
                        string? translatedContent = "";
                        ILrcFile? translated = null;
                        try
                        {
                            var translatedBase64 = (string?)jobj?["trans"];
                            if (!string.IsNullOrEmpty(translatedBase64))
                            {
                                translatedContent = Encoding.UTF8.GetString(Convert.FromBase64String(translatedBase64));
                                if (!string.IsNullOrEmpty(translatedContent))
                                {
                                    translated = LrcFile.FromText(translatedContent);
                                    if (translated.Lyrics.All(c => string.IsNullOrEmpty(c.Content)))
                                    {
                                        translated = null;
                                        translatedContent = "";
                                    }
                                }
                            }
                        }
                        catch { }

                        await SetLyricCacheAsync(cacheKey, lrcContent, translatedContent, cancellationToken);

                        return new NetEaseLyric(lrcFile, translated);
                    }
                }
                catch (Exception ex) when (!(ex is OperationCanceledException)) { }
            }
            return null;
        }

        #endregion QQ音乐api


        #region Utilities

        private static async Task<string> TryGetStringAsync(string uri, CancellationToken cancellationToken)
        {
            return await TryGetStringAsync(uri, "", cancellationToken);
        }

        private static async Task<string> TryGetStringAsync(string uri, string referer, CancellationToken cancellationToken)
        {
            if (client == null) client = new HttpClient();

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
            catch (Exception ex) when (!(ex is OperationCanceledException)) { }

            return String.Empty;
        }


        private static string GetMD5(string str)
        {
            using (var md5 = MD5.Create())
            {
                return md5.ComputeHash(Encoding.UTF8.GetBytes(str ?? ""))
                    .Aggregate(new StringBuilder(), (s, c) => s.AppendFormat("{0:x2}", c))
                    .ToString();
            }
        }

        private static string BuildSearchKey(string? name, string? artist)
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

        private static string GetSearchKey(string? str)
        {
            if (string.IsNullOrEmpty(str)) return string.Empty;

            str = replaceRegex.Replace(str, "");
            str = replaceSpaceRegex.Replace(str, " ");

            return str;
        }

        #endregion Utilities

    }

    public class NetEaseLyric
    {
        public NetEaseLyric(ILrcFile? lyric, ILrcFile? translated)
        {
            Lyric = lyric;
            Translated = translated;
        }

        public ILrcFile? Lyric { get; }

        public ILrcFile? Translated { get; }
    }
}
