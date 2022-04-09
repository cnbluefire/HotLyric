using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
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

        public static readonly ILrcFile EmptyLyric = LrcFile.FromText("[00:00.00]暂无歌词");
        public static readonly ILrcFile DownloadingLyric = LrcFile.FromText("[00:00.00]正在加载...");

        public static async Task<ILrcFile?> CreateLrcFileAsync(StorageFile file)
        {
            try
            {
                var text = await FileIO.ReadTextAsync(file, Windows.Storage.Streams.UnicodeEncoding.Utf8);
                return LrcFile.FromText(text);
            }
            catch { }

            return null;
        }

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

        public static async Task<NetEaseLyric?> GetLrcFileAsync(string searchKeyword, string neteaseMusicId, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(searchKeyword)) return null;

            var file = await GetLyricFromCacheAsync(searchKeyword, cancellationToken);
            if (file == null) file = await GetLyricFromNetworkAsync(searchKeyword, neteaseMusicId, cancellationToken);

            // 如果全是空行则认为歌词不存在
            if (file?.Lyric != null && file.Lyric.Lyrics.All(c => string.IsNullOrWhiteSpace(c.Content)))
            {
                return null;
            }

            return file;
        }

        private static async Task<NetEaseLyric?> GetLyricFromNetworkAsync(string searchKeyword, string? neteaseMusicId, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(searchKeyword)) return null;

            try
            {
                if (string.IsNullOrEmpty(neteaseMusicId))
                {
                    neteaseMusicId = await GetNeteaseMusicIdAsync(searchKeyword, cancellationToken);
                }
                else if (neteaseMusicId.StartsWith("ncm-", StringComparison.OrdinalIgnoreCase))
                {
                    neteaseMusicId = neteaseMusicId.Substring(4);
                }

                if (!string.IsNullOrEmpty(neteaseMusicId))
                {
                    try
                    {
                        var json2 = await TryGetStringAsync($"https://music.163.com/api/song/lyric?id={neteaseMusicId}&lv=-1&kv=1&tv=-1", cancellationToken);
                        if (string.IsNullOrEmpty(json2)) return null;

                        var jobj2 = JObject.Parse(json2);
                        var lrcContent = (string?)jobj2?["lrc"]?["lyric"];

                        if (string.IsNullOrEmpty(lrcContent)) return null;
                        var lrcFile = LrcFile.FromText(lrcContent);

                        if (lrcFile != null)
                        {
                            string? translatedContent = "";
                            ILrcFile? translated = null;
                            try
                            {
                                translatedContent = (string?)jobj2?["tlyric"]?["lyric"];
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

                            var md5 = GetMD5(searchKeyword);

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

                            return new NetEaseLyric(lrcFile, translated);
                        }
                    }
                    catch (Exception ex) when (!(ex is OperationCanceledException)) { }
                }
            }
            catch (Exception ex) when (!(ex is OperationCanceledException)) { }

            return null;
        }

        private static async Task<NetEaseLyric?> GetLyricFromCacheAsync(string searchKeyword, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(searchKeyword)) return null;

            var md5 = GetMD5(searchKeyword);

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

        private static string GetMD5(string str)
        {
            using (var md5 = MD5.Create())
            {
                return md5.ComputeHash(Encoding.UTF8.GetBytes(str ?? ""))
                    .Aggregate(new StringBuilder(), (s, c) => s.AppendFormat("{0:x2}", c))
                    .ToString();
            }
        }


        private static async Task<string?> GetNeteaseMusicIdAsync(string searchKeyword, CancellationToken cancellationToken)
        {
            try
            {
                var json = await TryGetStringAsync($"http://music.163.com/api/search/get/web?csrf_token=hlpretag=&hlposttag=&s={Uri.EscapeDataString(searchKeyword)}&type=1&offset=0&total=true&limit=10", cancellationToken);
                if (string.IsNullOrEmpty(json)) return null;

                var jobj = JObject.Parse(json);
                var arr = jobj?["result"]?["songs"] as JArray;

                if (arr?.Count > 0)
                {
                    Fastenshtein.Levenshtein lev = new Fastenshtein.Levenshtein(searchKeyword);
                    var minLen = int.MaxValue;
                    string? id = "";

                    foreach (var item in arr)
                    {
                        var _id = (string?)item["id"];

                        var sb = new StringBuilder((string?)item["name"] ?? "");
                        var artists = item.Value<JArray>("artists");
                        if (artists != null)
                        {
                            foreach (var artist in artists)
                            {
                                sb.Append(' ')
                                    .Append((string?)artist["name"] ?? "");
                            }
                        }

                        var len = lev.DistanceFrom(sb.ToString());
                        if (len < minLen)
                        {
                            minLen = len;
                            id = _id;
                        }
                    }

                    return minLen <= 10 ? id : null;
                }
            }
            catch (Exception ex) when (!(ex is OperationCanceledException)) { }

            return null;
        }

        private static async Task<string> TryGetStringAsync(string uri, CancellationToken cancellationToken)
        {
            if (client == null) client = new HttpClient();

            try
            {
                var resp = await client.GetAsync(uri, cancellationToken);
                return await resp.Content.ReadAsStringAsync();
            }
            catch (Exception ex) when (!(ex is OperationCanceledException)) { }

            return String.Empty;
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
