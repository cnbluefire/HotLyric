using HotLyric.Win32.Utils.LyricFiles;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace HotLyric.Win32.Utils.LrcProviders
{
    public class QQMusicLrcProvider : ILrcProvider
    {
        public string Name => "QQMusic";

        public async Task<Lyric?> GetByIdAsync(string songName, string? artists, object id, CancellationToken cancellationToken)
        {
            if (id is string _id && !string.IsNullOrEmpty(_id))
            {
                try
                {
                    var query = new Dictionary<string, string>()
                    {
                        ["songmid"] = _id,
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

                    var json = await LrcProviderHelper.TryGetStringAsync($"http://c.y.qq.com/lyric/fcgi-bin/fcg_query_lyric_new.fcg?{queryStr}", "https://y.qq.com/portal/player.html", cancellationToken);
                    if (string.IsNullOrEmpty(json)) return null;

                    var jobj = JObject.Parse(json);
                    var lrcBase64 = (string?)jobj?["lyric"];
                    if (string.IsNullOrEmpty(lrcBase64)) return null;

                    var lrcContent = "";
                    try
                    {
                        lrcContent = Encoding.UTF8.GetString(Convert.FromBase64String(lrcBase64));
                    }
                    catch (Exception ex)
                    {
                        HotLyric.Win32.Utils.LogHelper.LogError(ex);
                    }

                    if (string.IsNullOrEmpty(lrcContent)) return null;

                    try
                    {
                        lrcContent = System.Net.WebUtility.HtmlDecode(lrcContent);
                    }
                    catch (Exception ex)
                    {
                        HotLyric.Win32.Utils.LogHelper.LogError(ex);
                    }

                    string? translatedContent = "";
                    try
                    {
                        var translatedBase64 = (string?)jobj?["trans"];
                        if (!string.IsNullOrEmpty(translatedBase64))
                        {
                            translatedContent = Encoding.UTF8.GetString(Convert.FromBase64String(translatedBase64));
                        }
                    }
                    catch (Exception ex)
                    {
                        HotLyric.Win32.Utils.LogHelper.LogError(ex);
                    }

                    return Lyric.CreateClassicLyric(lrcContent, translatedContent, songName, artists);
                }
                catch (Exception ex) when (!(ex is OperationCanceledException))
                {
                    HotLyric.Win32.Utils.LogHelper.LogError(ex);
                }
            }
            return null;
        }

        public async Task<object?> GetIdAsync(string name, string? artists, CancellationToken cancellationToken)
        {
            try
            {
                var search = await Lyricify.Lyrics.Helpers.SearchHelper.Search(new Lyricify.Lyrics.Models.TrackMultiArtistMetadata()
                {
                    Artists = (artists ?? string.Empty).Split(", ").ToList(),
                    Title = name,
                }, Lyricify.Lyrics.Searchers.Searchers.QQMusic, Lyricify.Lyrics.Searchers.Helpers.CompareHelper.MatchType.Low);
                if (search is Lyricify.Lyrics.Searchers.QQMusicSearchResult match)
                {
                    return match.Mid;
                }
            }
            catch (Exception ex) when (!(ex is OperationCanceledException))
            {
                HotLyric.Win32.Utils.LogHelper.LogError(ex);
            }

            return null;
        }

    }
}
