using HotLyric.Win32.Utils.MediaSessions.SMTC;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HotLyric.Win32.Models.AppConfigurationModels.JsonModels
{
    public class AppConfigurationJsonModel
    {
        public MediaSessionAppJsonModel[]? MediaSessionApps { get; set; }
    }

    public class MediaSessionAppJsonModel
    {
        /// <summary>
        /// 提供 Media Session 的 App 的唯一 Id
        /// </summary>
        public string? AppId { get; set; }

        /// <summary>
        /// Media Session 类型
        /// </summary>
        public MediaSessionType SessionType { get; set; }

        /// <summary>
        /// 提供 Media Session 的 App 信息，Packaged app 可以不设置
        /// </summary>
        public MediaSessionAppInfoJsonModel? AppInfo { get; set; }

        /// <summary>
        /// 匹配应用信息
        /// </summary>
        public MediaSessionAppMatchJsonModel? Match { get; set; }

        /// <summary>
        /// Media Session 选项
        /// </summary>
        public MediaSessionAppOptionsJsonModel? Options { get; set; }
    }

    public class MediaSessionAppInfoJsonModel
    {
        /// <summary>
        /// App 名称
        /// </summary>
        public string? DisplayName { get; set; }

        /// <summary>
        /// App 图标
        /// </summary>
        public string? Icon { get; set; }

        /// <summary>
        /// 是否支持从热词中启动，默认为 true
        /// </summary>
        public bool SupportLaunch { get; set; } = true;

        /// <summary>
        /// 微软商店产品Id
        /// </summary>
        public string? MsStoreProductId { get; set; }
    }

    public class MediaSessionAppMatchJsonModel
    {
        /// <summary>
        /// 正则表达式
        /// </summary>
        public string? Regex { get; set; }

        /// <summary>
        /// 支持的最低版本
        /// </summary>
        public string? MinSupportedVersion { get; set; }
    }

    public class MediaSessionAppOptionsJsonModel
    {
        /// <summary>
        /// YesPlayMusic 的媒体信息服务器地址
        /// </summary>
        public string? MediaInfoServer { get; set; }

        /// <summary>
        /// 正在播放的进度获取模式
        /// </summary>
        public MediaSessionPositionMode PositionMode { get; set; }

        /// <summary>
        /// 默认的歌词提供器
        /// </summary>
        public string? DefaultLrcProvider { get; set; }

        /// <summary>
        /// 是否将繁体中文转换为简体中文
        /// </summary>
        public bool ConvertToSimpleChinese { get; set; }

        /// <summary>
        /// 读取媒体信息的模式
        /// </summary>
        public MediaPropertiesMode MediaPropertiesMode { get; set; }

    }
}
