using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Media.Control;

namespace HotLyric.Win32.Utils.MediaSessions.SMTC
{
    public static class SMTCApps
    {
        private static readonly IReadOnlyDictionary<string, SMTCApp> allApps = new Dictionary<string, SMTCApp>()
        {
            ["HyPlayer"] = new SMTCApp(
                appId: "9n5td916686k",
                packageFamilyNamePrefix: "48848aaaaaaccd.HyPlayer_",
                hasStoreUri: true,
                positionMode: SMTCAppPositionMode.FromApp,
                defaultLrcProvider: "NetEase",
                createMediaPropertiesAction: NCMClientCreateMediaProperties),

            ["LyricEase"] = new SMTCApp(
                appId: "9n1mkdf0f4gt",
                packageFamilyNamePrefix: "17588BrandonWong.LyricEase_",
                hasStoreUri: true,
                positionMode: SMTCAppPositionMode.FromApp,
                defaultLrcProvider: "NetEase",
                createMediaPropertiesAction: NCMClientCreateMediaProperties),

            ["Spotify"] = new SMTCApp(
                appId: "9ncbcszsjrsb",
                packageFamilyNamePrefix: "Spotify.exe",
                hasStoreUri: true,
                positionMode: SMTCAppPositionMode.FromAppAndUseTimer,
                customName: "Spotify",
                customAppIcon: new BitmapImage(new Uri("ms-appx:///Assets/SpotifyIcon.png", UriKind.RelativeOrAbsolute)),
                supportLaunch: false,
                defaultLrcProvider: "QQMusic",
                convertToSimpleChinese: true,
                createMediaPropertiesAction: DefaultCreateMediaProperties),

            ["Spotify_Store"] = new SMTCApp(
                appId: "9ncbcszsjrsb",
                packageFamilyNamePrefix: "SpotifyAB.SpotifyMusic_",
                hasStoreUri: true,
                positionMode: SMTCAppPositionMode.FromAppAndUseTimer,
                customAppIcon: new BitmapImage(new Uri("ms-appx:///Assets/SpotifyIcon.png", UriKind.RelativeOrAbsolute)),
                defaultLrcProvider: "QQMusic",
                createMediaPropertiesAction: DefaultCreateMediaProperties),

            ["NeteaseMusic"] = new SMTCApp(
                appId: "9nblggh6g0jf",
                packageFamilyNamePrefix: "1F8B0F94.122165AE053F_",
                hasStoreUri: true,
                positionMode: SMTCAppPositionMode.OnlyUseTimer,
                defaultLrcProvider: "NetEase",
                convertToSimpleChinese: false,
                createMediaPropertiesAction: DefaultCreateMediaProperties),

            ["QQMusic"] = new SMTCApp(
                appId: "9wzdncrfj1q1",
                packageFamilyNamePrefix: "903DB504.QQWP_",
                hasStoreUri: true,
                positionMode: SMTCAppPositionMode.FromApp,
                defaultLrcProvider: "QQMusic",
                convertToSimpleChinese: false,
                createMediaPropertiesAction: NCMClientCreateMediaProperties),

            ["QQMusicPreview"] = new SMTCApp(
                appId: "",
                packageFamilyNamePrefix: "903DB504.12708F202F598_",
                hasStoreUri: false,
                positionMode: SMTCAppPositionMode.FromApp,
                defaultLrcProvider: "QQMusic",
                convertToSimpleChinese: false,
                createMediaPropertiesAction: DefaultCreateMediaProperties),

            ["Groove"] = new SMTCApp(
                appId: "9wzdncrfj3pt",
                packageFamilyNamePrefix: "Microsoft.ZuneMusic_",
                hasStoreUri: true,
                positionMode: SMTCAppPositionMode.FromAppAndUseTimer,
                defaultLrcProvider: "NeteaseMusic",
                convertToSimpleChinese: true,
                minSupportedVersion: new Version(11, 2111, 0, 0),
                createMediaPropertiesAction: DefaultCreateMediaProperties),

            ["Groove_Old"] = new SMTCApp(
                appId: "9wzdncrfj3pt",
                packageFamilyNamePrefix: "Microsoft.ZuneMusic_",
                hasStoreUri: true,
                positionMode: SMTCAppPositionMode.OnlyUseTimer,
                defaultLrcProvider: "NeteaseMusic",
                convertToSimpleChinese: true,
                createMediaPropertiesAction: DefaultCreateMediaProperties),

            ["Foobar2000"] = new SMTCApp(
                appId: "foobar2000.exe",
                packageFamilyNamePrefix: "foobar2000.exe",
                hasStoreUri: false,
                positionMode: SMTCAppPositionMode.OnlyUseTimer,
                customName: "Foobar2000",
                customAppIcon: new BitmapImage(new Uri("ms-appx:///Assets/Foobar2kIcon.png", UriKind.RelativeOrAbsolute)),
                supportLaunch: false,
                defaultLrcProvider: "NeteaseMusic",
                convertToSimpleChinese: true,
                createMediaPropertiesAction: DefaultCreateMediaProperties),

            ["YesPlayerMusic"] = new SMTCApp(
                appId: "com.electron.yesplaymusic",
                packageFamilyNamePrefix: "com.electron.yesplaymusic",
                hasStoreUri: false,
                positionMode: SMTCAppPositionMode.FromApp,
                customName: "YesPlayerMusic",
                customAppIcon: new BitmapImage(new Uri("ms-appx:///Assets/YesPlayerMusicIcon.png", UriKind.RelativeOrAbsolute)),
                supportLaunch: false,
                defaultLrcProvider: "NeteaseMusic",
                convertToSimpleChinese: false,
                createMediaPropertiesAction: DefaultCreateMediaProperties),

            ["YesPlayerMusic_Portable"] = new SMTCApp(
                appId: "YesPlayMusic.exe",
                packageFamilyNamePrefix: "YesPlayMusic.exe",
                hasStoreUri: false,
                positionMode: SMTCAppPositionMode.FromApp,
                customName: "YesPlayerMusic",
                customAppIcon: new BitmapImage(new Uri("ms-appx:///Assets/YesPlayerMusicIcon.png", UriKind.RelativeOrAbsolute)),
                supportLaunch: false,
                defaultLrcProvider: "NeteaseMusic",
                convertToSimpleChinese: false,
                createMediaPropertiesAction: DefaultCreateMediaProperties),

            ["AppleMusic"] = new SMTCApp(
                appId: "9pfhdd62mxs1",
                packageFamilyNamePrefix: "AppleInc.AppleMusicWin_",
                hasStoreUri: true,
                positionMode: SMTCAppPositionMode.FromApp,
                defaultLrcProvider: "NetEase",
                createMediaPropertiesAction: AppleMusicCreateMediaProperties),
            ["PlanetMusicPlayer"] = new SMTCApp(
                appId: "t51rdba1cnx74",
                packageFamilyNamePrefix: "00d00666-37d5-4c47-b6c6-e62aa3f8a652_",
                hasStoreUri: false,
                positionMode: SMTCAppPositionMode.FromAppAndUseTimer,
                defaultLrcProvider: "QQMusic",
                convertToSimpleChinese: true,
                createMediaPropertiesAction: DefaultCreateMediaProperties),
        };

        public static IReadOnlyDictionary<string, SMTCApp> AllApps => allApps;

        public static SMTCApp HyPlayer => allApps["HyPlayer"];

        public static SMTCApp LyricEase => allApps["LyricEase"];

        private static MediaSessionMediaProperties? DefaultCreateMediaProperties(GlobalSystemMediaTransportControlsSessionMediaProperties? mediaProperties)
        {
            if (mediaProperties == null) return null;

            var genres = mediaProperties.Genres?.ToArray();

            return new MediaSessionMediaProperties(
                mediaProperties.AlbumArtist,
                mediaProperties.AlbumTitle,
                mediaProperties.AlbumTrackCount,
                mediaProperties.Artist,
                "",
                "",
                genres ?? Array.Empty<string>(),
                mediaProperties.Subtitle,
                mediaProperties.Title,
                mediaProperties.TrackNumber);
        }

        private static MediaSessionMediaProperties? AppleMusicCreateMediaProperties(GlobalSystemMediaTransportControlsSessionMediaProperties? mediaProperties)
        {
            if (mediaProperties == null) return null;

            var albumArtist = mediaProperties.AlbumArtist;
            var arr = albumArtist.Split(" — ");

            var artist = mediaProperties.Artist;
            var album = mediaProperties.Title;

            if (arr.Length >= 2)
            {
                if (string.IsNullOrEmpty(artist)) artist = arr[0];
                if (string.IsNullOrEmpty(album)) album = arr[1];
            }
            else if (string.IsNullOrEmpty(artist))
            {
                artist = albumArtist;
            }

            var genres = mediaProperties.Genres?.ToArray();

            return new MediaSessionMediaProperties(
                albumArtist,
                album,
                mediaProperties.AlbumTrackCount,
                artist,
                "",
                "",
                genres ?? Array.Empty<string>(),
                mediaProperties.Subtitle,
                mediaProperties.Title,
                mediaProperties.TrackNumber);
        }
        private static MediaSessionMediaProperties? NCMClientCreateMediaProperties(GlobalSystemMediaTransportControlsSessionMediaProperties? mediaProperties)
        {
            if (mediaProperties == null) return null;

            int skip = 0;

            var neteaseMusicId = string.Empty;
            var localLrcPath = string.Empty;

            var genres = mediaProperties.Genres?.ToArray();

            if (genres != null)
            {
                if (genres.Length > 0 && genres[0]?.StartsWith("ncm-", StringComparison.OrdinalIgnoreCase) == true)
                {
                    neteaseMusicId = genres[0].Substring(4);
                    skip++;
                }

                if (genres.Length > 1
                    && !string.IsNullOrEmpty(genres[1])
                    && genres[1].Trim() is string path
                    && !System.IO.Path.IsPathRooted(path))
                {
                    localLrcPath = path;
                    skip++;
                }
            }

            if (skip > 0)
            {
                genres = genres?.Skip(skip).ToArray();
            }

            return new MediaSessionMediaProperties(
                mediaProperties.AlbumArtist,
                mediaProperties.AlbumTitle,
                mediaProperties.AlbumTrackCount,
                mediaProperties.Artist,
                neteaseMusicId,
                localLrcPath,
                genres ?? Array.Empty<string>(),
                mediaProperties.Subtitle,
                mediaProperties.Title,
                mediaProperties.TrackNumber);
        }
    }

}
