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
                    var lyric = await Lyricify.Lyrics.Helpers.ProviderHelper.QQMusicApi.GetLyric(_id).WaitAsync(cancellationToken);
                    if (string.IsNullOrEmpty(lyric?.Lyric)) return null;

                    return Lyric.CreateClassicLyric(lyric.Lyric, lyric.Trans, songName, artists);
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
