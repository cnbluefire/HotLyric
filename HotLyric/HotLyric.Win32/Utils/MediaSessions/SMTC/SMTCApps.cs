using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Media;
using System.Windows.Media.Imaging;

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
                defaultLrcProvider: "NetEase"),

            ["LyricEase"] = new SMTCApp(
                appId: "9n1mkdf0f4gt",
                packageFamilyNamePrefix: "17588BrandonWong.LyricEase_",
                hasStoreUri: true,
                positionMode: SMTCAppPositionMode.FromApp,
                defaultLrcProvider: "NetEase"),

            ["Spotify"] = new SMTCApp(
                appId: "9ncbcszsjrsb",
                packageFamilyNamePrefix: "Spotify.exe",
                hasStoreUri: true,
                positionMode: SMTCAppPositionMode.FromAppAndUseTimer,
                customName: "Spotify",
                customAppIcon: new BitmapImage(new Uri("/Assets/SpotifyIcon.png", UriKind.RelativeOrAbsolute)),
                supportLaunch: false,
                defaultLrcProvider: "QQMusic",
                convertToSimpleChinese: true),

            ["Spotify_Store"] = new SMTCApp(
                appId: "9ncbcszsjrsb",
                packageFamilyNamePrefix: "SpotifyAB.SpotifyMusic_",
                hasStoreUri: true,
                positionMode: SMTCAppPositionMode.FromAppAndUseTimer,
                customAppIcon: new BitmapImage(new Uri("/Assets/SpotifyIcon.png", UriKind.RelativeOrAbsolute)),
                defaultLrcProvider: "QQMusic"),

            ["NeteaseMusic"] = new SMTCApp(
                appId: "9nblggh6g0jf",
                packageFamilyNamePrefix: "1F8B0F94.122165AE053F_",
                hasStoreUri: true,
                positionMode: SMTCAppPositionMode.OnlyUseTimer,
                defaultLrcProvider: "NetEase",
                convertToSimpleChinese: false),

            ["QQMusic"] = new SMTCApp(
                appId: "9wzdncrfj1q1",
                packageFamilyNamePrefix: "903DB504.QQWP_",
                hasStoreUri: true,
                positionMode: SMTCAppPositionMode.OnlyUseTimer,
                defaultLrcProvider: "QQMusic",
                convertToSimpleChinese: false),

            ["QQMusicPreview"] = new SMTCApp(
                appId: "",
                packageFamilyNamePrefix: "903DB504.12708F202F598_",
                hasStoreUri: false,
                positionMode: SMTCAppPositionMode.OnlyUseTimer,
                defaultLrcProvider: "QQMusic",
                convertToSimpleChinese: false),

            ["Groove"] = new SMTCApp(
                appId: "9wzdncrfj3pt",
                packageFamilyNamePrefix: "Microsoft.ZuneMusic_",
                hasStoreUri: true,
                positionMode: SMTCAppPositionMode.FromAppAndUseTimer,
                defaultLrcProvider: "NeteaseMusic",
                convertToSimpleChinese: true,
                minSupportedVersion: new Version(11, 2111, 0, 0)),

            ["Groove_Old"] = new SMTCApp(
                appId: "9wzdncrfj3pt",
                packageFamilyNamePrefix: "Microsoft.ZuneMusic_",
                hasStoreUri: true,
                positionMode: SMTCAppPositionMode.OnlyUseTimer,
                defaultLrcProvider: "NeteaseMusic",
                convertToSimpleChinese: true),

            ["Foobar2000"] = new SMTCApp(
                appId: "foobar2000.exe",
                packageFamilyNamePrefix: "foobar2000.exe",
                hasStoreUri: false,
                positionMode: SMTCAppPositionMode.OnlyUseTimer,
                customName: "Foobar2000",
                customAppIcon: new BitmapImage(new Uri("/Assets/Foobar2kIcon.png", UriKind.RelativeOrAbsolute)),
                supportLaunch: false,
                defaultLrcProvider: "NeteaseMusic",
                convertToSimpleChinese: true),

            ["YesPlayerMusic"] = new SMTCApp(
                appId: "com.electron.yesplaymusic",
                packageFamilyNamePrefix: "com.electron.yesplaymusic",
                hasStoreUri: false,
                positionMode: SMTCAppPositionMode.FromApp,
                customName: "YesPlayerMusic",
                customAppIcon: new BitmapImage(new Uri("/Assets/YesPlayerMusicIcon.png", UriKind.RelativeOrAbsolute)),
                supportLaunch: false,
                defaultLrcProvider: "NeteaseMusic",
                convertToSimpleChinese: false),

            ["YesPlayerMusic_Portable"] = new SMTCApp(
                appId: "YesPlayMusic.exe",
                packageFamilyNamePrefix: "YesPlayMusic.exe",
                hasStoreUri: false,
                positionMode: SMTCAppPositionMode.FromApp,
                customName: "YesPlayerMusic",
                customAppIcon: new BitmapImage(new Uri("/Assets/YesPlayerMusicIcon.png", UriKind.RelativeOrAbsolute)),
                supportLaunch: false,
                defaultLrcProvider: "NeteaseMusic",
                convertToSimpleChinese: false),
        };

        public static IReadOnlyDictionary<string, SMTCApp> AllApps => allApps;

        public static SMTCApp HyPlayer => allApps["HyPlayer"];

        public static SMTCApp LyricEase => allApps["LyricEase"];
    }

}
