using System;
using System.Collections.Generic;
using System.Text;
using Windows.Media;

namespace HotLyric.Win32.Utils.MediaSessions
{
    public class MediaSessionMediaProperties
    {
        public MediaSessionMediaProperties(string albumArtist, string albumTitle, int albumTrackCount, string artist, IReadOnlyList<string> genres, string subtitle, string title, int trackNumber)
        {
            AlbumArtist = albumArtist;
            AlbumTitle = albumTitle;
            AlbumTrackCount = albumTrackCount;
            Artist = artist;
            Genres = genres;
            Subtitle = subtitle;
            Title = title;
            TrackNumber = trackNumber;
        }

        public string AlbumArtist { get; } = "";

        public string AlbumTitle { get; } = "";

        public int AlbumTrackCount { get; }

        public string Artist { get; } = "";

        public IReadOnlyList<string> Genres { get; } = Array.Empty<string>();

        public string Subtitle { get; } = "";

        public string Title { get; } = "";

        public int TrackNumber { get; } = 0;
    }
}
