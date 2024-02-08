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
    public class NetEaseLrcProvider : ILrcProvider
    {
        public string Name => "NetEase";

        public async Task<Lyric?> GetByIdAsync(string songName, string? artists, object id, CancellationToken cancellationToken)
        {
            if (id is string _id && !string.IsNullOrEmpty(_id))
            {
                try
                {
                    var json = await LrcProviderHelper.TryGetStringAsync($"https://music.163.com/api/song/lyric?id={_id}&lv=-1&kv=1&tv=-1", cancellationToken);
                    if (string.IsNullOrEmpty(json)) return null;

                    var jobj = JObject.Parse(json);
                    var lrcContent = (string?)jobj?["lrc"]?["lyric"];

                    if (string.IsNullOrEmpty(lrcContent)) return null;

                    string? translatedContent = "";
                    try
                    {
                        translatedContent = (string?)jobj?["tlyric"]?["lyric"];
                    }
                    catch (Exception ex)
                    {
                        HotLyric.Win32.Utils.LogHelper.LogError(ex);
                    }

                    return Lyric.CreateClassicLyric(lrcContent!, translatedContent, songName, artists);
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
                }, Lyricify.Lyrics.Searchers.Searchers.Netease, Lyricify.Lyrics.Searchers.Helpers.CompareHelper.MatchType.Low);
                if (search is Lyricify.Lyrics.Searchers.NeteaseSearchResult match)
                {
                    return match.Id;
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
