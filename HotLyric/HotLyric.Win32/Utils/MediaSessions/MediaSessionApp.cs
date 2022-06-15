using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Media;

namespace HotLyric.Win32.Utils.MediaSessions
{
    public class MediaSessionApp
    {
        public MediaSessionApp(
            string appId,
            string? customName = null,
            ImageSource? customAppIcon = null,
            string? defaultLrcProvider = null,
            bool convertToSimpleChinese = false,
            Version? minSupportedVersion = null)
        {
            AppId = appId;
            CustomName = customName;
            CustomAppIcon = customAppIcon;
            DefaultLrcProvider = defaultLrcProvider;
            ConvertToSimpleChinese = convertToSimpleChinese;
            MinSupportedVersion = minSupportedVersion;
        }

        public string AppId { get; }

        public string? CustomName { get; }

        public ImageSource? CustomAppIcon { get; }

        public string? DefaultLrcProvider { get; }

        public bool ConvertToSimpleChinese { get; }

        public Version? MinSupportedVersion { get; }
    }
}
