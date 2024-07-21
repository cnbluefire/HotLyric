using DirectN;
using HotLyric.Win32.Utils;
using HotLyric.Win32.Utils.MediaSessions.SMTC;
using Microsoft.UI.Xaml.Media;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace HotLyric.Win32.Models.AppConfigurationModels
{
    public class AppConfigurationModel
    {
        public static AppConfigurationModel? CreateFromJson(string configureSource, string json)
        {
            JsonModels.AppConfigurationJsonModel? jsonModel = null;
            try
            {
                jsonModel = JsonConvert.DeserializeObject<JsonModels.AppConfigurationJsonModel>(json);
            }
            catch { }

            if (jsonModel != null)
            {
                var list = new List<MediaSessionAppModel>();
                var hash = new HashSet<string>();

                var template = configureSource.Replace("configuration.json", "{{data}}");

                if (jsonModel.MediaSessionApps != null)
                {
                    foreach (var mediaSessionApp in jsonModel.MediaSessionApps)
                    {
                        if (mediaSessionApp != null
                            && !string.IsNullOrEmpty(mediaSessionApp.AppId)
                            && !string.IsNullOrEmpty(mediaSessionApp.Match?.Regex))
                        {
                            Version? minSupportedVersion = null;

                            if (string.IsNullOrEmpty(mediaSessionApp.Match.MinSupportedVersion))
                            {
                                minSupportedVersion = new Version(0, 0, 0, 0);
                            }
                            else if (!Version.TryParse(mediaSessionApp.Match.MinSupportedVersion, out minSupportedVersion))
                            {
                                minSupportedVersion = null;
                            }

                            Uri? mediaInfoServer = null;
                            bool mediaInfoServerFlag = false;

                            if (mediaSessionApp.SessionType == MediaSessionType.YesPlayMusic)
                            {
                                if (!(mediaInfoServerFlag = Uri.TryCreate(mediaSessionApp.Options?.MediaInfoServer, UriKind.Absolute, out mediaInfoServer)))
                                {
                                    mediaInfoServer = null;
                                }
                            }
                            else
                            {
                                mediaInfoServerFlag = true;
                            }

                            if (minSupportedVersion != null
                                && mediaInfoServerFlag
                                && hash.Add(mediaSessionApp.AppId))
                            {
                                Uri? icon = null;
                                if (!string.IsNullOrEmpty(mediaSessionApp.AppInfo?.Icon))
                                {
                                    if (!Uri.TryCreate(mediaSessionApp.AppInfo.Icon, UriKind.Absolute, out icon))
                                    {
                                        var iconRelativePath = mediaSessionApp.AppInfo.Icon.TrimStart('/');
                                        if (!Uri.TryCreate(template.Replace("{{data}}", iconRelativePath), UriKind.Absolute, out icon))
                                        {
                                            icon = null;
                                        }
                                    }
                                }

                                if (mediaSessionApp.SessionType == MediaSessionType.SMTC_PackagedApp
                                    || mediaSessionApp.SessionType == MediaSessionType.SMTC_UnPackagedApp
                                    || mediaSessionApp.SessionType == MediaSessionType.YesPlayMusic)
                                {
                                    list.Add(new MediaSessionAppModel(
                                        mediaSessionApp.AppId,
                                        mediaSessionApp.SessionType,
                                        new MediaSessionAppInfoModel(
                                            mediaSessionApp.AppInfo?.DisplayName,
                                            icon,
                                            mediaSessionApp.AppInfo?.MsStoreProductId),
                                        new MediaSessionAppMatchModel(
                                            new Regex(mediaSessionApp.Match.Regex),
                                            minSupportedVersion),
                                        new MediaSessionAppOptionsModel(
                                            mediaInfoServer,
                                            mediaSessionApp.Options?.PositionMode ?? MediaSessionPositionMode.FromApp,
                                            mediaSessionApp.Options?.DefaultLrcProvider ?? "NetEase",
                                            mediaSessionApp.Options?.ConvertToSimpleChinese ?? false,
                                            mediaSessionApp.Options?.MediaPropertiesMode ?? MediaPropertiesMode.Default)));
                                }
                            }
                        }
                    }
                }

                return new AppConfigurationModel()
                {
                    MediaSessionApps = list
                };
            }

            return null;
        }

        public IReadOnlyList<MediaSessionAppModel> MediaSessionApps { get; private set; } = null!;


        /// <param name="AppId"> 提供 Media Session 的 App 的唯一 Id </param>
        /// <param name="SessionType"> Media Session 类型 </param>
        /// <param name="AppInfo"> 提供 Media Session 的 App 信息，Packaged app 可以不设置 </param>
        /// <param name="Match"> 匹配应用信息 </param>
        /// <param name="Options"> Media Session 选项 </param>
        public record class MediaSessionAppModel(
            string? AppId,
            MediaSessionType SessionType,
            MediaSessionAppInfoModel? AppInfo,
            MediaSessionAppMatchModel Match,
            MediaSessionAppOptionsModel Options);



        /// <param name="DisplayName"> App 名称 </param>
        /// <param name="Icon"> App 图标 </param>
        /// <param name="MsStoreProductId"> 微软商店产品Id </param>
        public record class MediaSessionAppInfoModel(
            string? DisplayName,
            Uri? Icon,
            string? MsStoreProductId);


        /// <param name="Regex"> 正则表达式 </param>
        /// <param name="MinSupportedVersion"> 支持的最低版本 </param>
        public record class MediaSessionAppMatchModel(
            Regex Regex,
            Version? MinSupportedVersion);


        /// <param name="MediaInfoServer"> YesPlayMusic 的媒体信息服务器地址 </param>
        /// <param name="PositionMode"> 正在播放的进度获取模式 </param>
        /// <param name="DefaultLrcProvider"> 默认的歌词提供器 </param>
        /// <param name="ConvertToSimpleChinese"> 是否将繁体中文转换为简体中文 </param>
        /// <param name="MediaPropertiesMode"> 读取媒体信息的模式 </param>
        public record class MediaSessionAppOptionsModel(
            Uri? MediaInfoServer,
            MediaSessionPositionMode PositionMode,
            string? DefaultLrcProvider,
            bool ConvertToSimpleChinese,
            MediaPropertiesMode MediaPropertiesMode);
    }

    public class MediaSessionAppModel
    {
        public string? AppId { get; set; }

        public string? CustomName { get; set; }

        public ImageSource? CustomAppIcon { get; set; }

        public Version? MinSupportedVersion { get; set; }

        public MediaSessionType SessionType { get; set; }

        public MediaSessionAppConfigure? Configure { get; set; }
    }

    public enum MediaSessionType
    {
        SMTC_PackagedApp,
        SMTC_UnPackagedApp,
        YesPlayMusic
    }

    //public enum SMTCAppPositionMode
    //{
    //    FromApp,
    //    FromAppAndUseTimer,
    //    OnlyUseTimer
    //}

    public enum MediaPropertiesMode
    {
        Default,
        NCMClient,
        AppleMusic
    }

    public enum MediaSessionPositionMode
    {
        FromApp,
        FromAppAndUseTimer,
        OnlyUseTimer
    }

    public class MediaSessionAppConfigure
    {
        public Uri? StoreUri { get; set; }

        public string? PackageFamilyNamePrefix { get; set; }

        public MediaSessionPositionMode PositionMode { get; set; }

        public bool SupportLaunch { get; set; }

        public string? DefaultLrcProvider { get; set; }

        public bool ConvertToSimpleChinese { get; set; }

        public MediaPropertiesMode MediaPropertiesMode { get; set; }
    }
}
