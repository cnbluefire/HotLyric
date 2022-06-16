using System;
using System.Collections.Generic;
using System.Text;
using Windows.Media;

namespace HotLyric.Win32.Utils.MediaSessions
{
    public class MediaSessionMediaProperties
    {
        public MediaSessionMediaProperties(string albumArtist, string albumTitle, int albumTrackCount, string artist, string neteaseMusicId, string localLrcPath, IReadOnlyList<string> genres, string subtitle, string title, int trackNumber)
        {
            AlbumArtist = albumArtist;
            AlbumTitle = albumTitle;
            AlbumTrackCount = albumTrackCount;
            Artist = artist;
            NeteaseMusicId = neteaseMusicId;
            LocalLrcPath = localLrcPath;
            Genres = genres;
            Subtitle = subtitle;
            Title = title;
            TrackNumber = trackNumber;
        }

        public string AlbumArtist { get; } = "";

        public string AlbumTitle { get; } = "";

        public int AlbumTrackCount { get; }

        public string Artist { get; } = "";

        public string NeteaseMusicId { get; } = "";

        public string LocalLrcPath { get; } = "";

        public IReadOnlyList<string> Genres { get; } = Array.Empty<string>();

        public string Subtitle { get; } = "";

        public string Title { get; } = "";

        public int TrackNumber { get; } = 0;
    }
}
