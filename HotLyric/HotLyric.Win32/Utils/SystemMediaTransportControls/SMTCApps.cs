using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace HotLyric.Win32.Utils.SystemMediaTransportControls
{
    public static class SMTCApps
    {
        private static readonly IReadOnlyDictionary<string, SMTCApp> allApps = new Dictionary<string, SMTCApp>()
        {
            ["HyPlayer"] = new SMTCApp(
                "48848aaaaaaccd.HyPlayer_",
                "9n5td916686k",
                SMTCAppPositionMode.FromApp,
                defaultLrcProvider: "NetEase"),

            ["LyricEase"] = new SMTCApp(
                "17588BrandonWong.LyricEase_",
                "9n1mkdf0f4gt",
                SMTCAppPositionMode.FromApp,
                defaultLrcProvider: "NetEase"),

            ["Spotify"] = new SMTCApp(
                "Spotify.exe",
                "9ncbcszsjrsb",
                SMTCAppPositionMode.FromAppAndUseTimer,
                "Spotify",
                new BitmapImage(new Uri("/Assets/SpotifyIcon.png", UriKind.RelativeOrAbsolute)),
                false,
                "QQMusic",
                true),

            ["Spotify_Store"] = new SMTCApp(
                "SpotifyAB.SpotifyMusic_",
                "9ncbcszsjrsb",
                SMTCAppPositionMode.FromAppAndUseTimer,
                customAppIcon: new BitmapImage(new Uri("/Assets/SpotifyIcon.png", UriKind.RelativeOrAbsolute)),
                defaultLrcProvider: "QQMusic"),

            ["NeteaseMusic"] = new SMTCApp(
                "1F8B0F94.122165AE053F_",
                "9nblggh6g0jf",
                SMTCAppPositionMode.OnlyUseTimer,
                defaultLrcProvider: "NetEase",
                convertToSimpleChinese: false),

            ["QQMusic"] = new SMTCApp(
                "903DB504.QQWP_",
                "9wzdncrfj1q1",
                SMTCAppPositionMode.OnlyUseTimer,
                defaultLrcProvider: "QQMusic",
                convertToSimpleChinese: false),

            ["Groove"] = new SMTCApp(
                "Microsoft.ZuneMusic_",
                "9wzdncrfj3pt",
                SMTCAppPositionMode.FromAppAndUseTimer,
                defaultLrcProvider: "NeteaseMusic",
                convertToSimpleChinese: true,
                minSupportedVersion: new Version(11, 2111, 0, 0)),

            ["Groove_Old"] = new SMTCApp(
                "Microsoft.ZuneMusic_",
                "9wzdncrfj3pt",
                SMTCAppPositionMode.OnlyUseTimer,
                defaultLrcProvider: "NeteaseMusic",
                convertToSimpleChinese: true),

            ["Foobar2000"] = new SMTCApp(
                "foobar2000.exe",
                "",
                SMTCAppPositionMode.OnlyUseTimer,
                customName: "Foobar2000",
                new BitmapImage(new Uri("/Assets/Foobar2kIcon.png", UriKind.RelativeOrAbsolute)),
                supportLaunch: false,
                defaultLrcProvider: "NeteaseMusic",
                convertToSimpleChinese: true)
        };

        public static IReadOnlyDictionary<string, SMTCApp> AllApps => allApps;

        public static SMTCApp HyPlayer => allApps["HyPlayer"];

        public static SMTCApp LyricEase => allApps["LyricEase"];
    }

    public class SMTCApp
    {
        public SMTCApp(
            string packageFamilyNamePrefix,
            string productId,
            SMTCAppPositionMode positionMode,
            string? customName = null,
            ImageSource? customAppIcon = null,
            bool supportLaunch = true,
            string? defaultLrcProvider = null,
            bool convertToSimpleChinese = false,
            Version? minSupportedVersion = null)
        {
            PackageFamilyNamePrefix = packageFamilyNamePrefix;
            ProductId = productId;
            StoreUri = new Uri($"ms-windows-store://pdp/?productid={ProductId}&mode=mini");
            PositionMode = positionMode;
            CustomName = customName;
            CustomAppIcon = customAppIcon;
            SupportLaunch = supportLaunch;
            DefaultLrcProvider = defaultLrcProvider;
            ConvertToSimpleChinese = convertToSimpleChinese;
            MinSupportedVersion = minSupportedVersion;
        }

        public Uri StoreUri { get; }

        public string PackageFamilyNamePrefix { get; }

        public string ProductId { get; }

        public SMTCAppPositionMode PositionMode { get; }

        public string? CustomName { get; }

        public ImageSource? CustomAppIcon { get; }

        public bool SupportLaunch { get; }

        public string? DefaultLrcProvider { get; }

        public bool ConvertToSimpleChinese { get; }

        public Version? MinSupportedVersion { get; }
    }

    public enum SMTCAppPositionMode
    {
        FromApp,
        FromAppAndUseTimer,
        OnlyUseTimer
    }
}
