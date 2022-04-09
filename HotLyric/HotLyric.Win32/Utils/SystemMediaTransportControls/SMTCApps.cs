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
            ["HyPlayer"] = new SMTCApp("48848aaaaaaccd.HyPlayer_", "9n5td916686k"),
            ["LyricEase"] = new SMTCApp("17588BrandonWong.LyricEase_", "9n1mkdf0f4gt"),
            ["Spotify"] = new SMTCApp(
                "Spotify.exe",
                "9ncbcszsjrsb",
                true,
                "Spotify",
                new BitmapImage(new Uri("/Assets/SpotifyIcon.png", UriKind.RelativeOrAbsolute)),
                false)
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
            bool useTimer = false,
            string? customName = null,
            ImageSource? customAppIcon = null,
            bool supportLaunch = true)
        {
            PackageFamilyNamePrefix = packageFamilyNamePrefix;
            ProductId = productId;
            StoreUri = new Uri($"ms-windows-store://pdp/?productid={ProductId}&mode=mini");
            UseTimer = useTimer;
            CustomName = customName;
            CustomAppIcon = customAppIcon;
            SupportLaunch = supportLaunch;
        }

        public Uri StoreUri { get; }

        public string PackageFamilyNamePrefix { get; }

        public string ProductId { get; }

        public bool UseTimer { get; }

        public string? CustomName { get; }

        public ImageSource? CustomAppIcon { get; }

        public bool SupportLaunch { get; }
    }
}
