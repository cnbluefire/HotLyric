# 热词

为其他播放器提供桌面歌词。

下载地址: [https://www.microsoft.com/store/productId/9MXFFHVQVBV9](https://www.microsoft.com/store/productId/9MXFFHVQVBV9)

## 软件截图
![app](https://github.com/cnbluefire/HotLyric/blob/main/assets/app.png)

## 支持的播放器   
|播放器|支持程度|
|---|---|
|HyPlayer|完全支持
|LyricEase|完全支持
|[Spotify](https://www.spotify.com/)|歌词可能匹配不准确<sup><a href="#ref1">1</a></sup>
|[网易云音乐 UWP](https://github.com/JasonWei512/NetEase-Cloud-Music-UWP-Repack)<sup><a href="#ref2">2</a></sup>|歌词可能匹配不准确<sup><a href="#ref1">1</a></sup> 无法获取进度<sup><a href="#ref3">3</a></sup>
|[QQ音乐 UWP](https://www.microsoft.com/store/productId/9WZDNCRFJ1Q1)|歌词可能匹配不准确<sup><a href="#ref1">1</a></sup> 无法获取进度<sup><a href="#ref3">3</a></sup> 无法获取歌曲信息<sup><a href="#ref4">4</a></sup>
|[媒体播放器（Groove 音乐）](https://www.microsoft.com/store/productId/9WZDNCRFJ3PT)|歌词可能匹配不准确<sup><a href="#ref1">1</a>
|[Foobar2000 (v1.5.1+)](https://www.foobar2000.org/)|歌词可能匹配不准确<sup><a href="#ref1">1</a></sup> 无法获取进度<sup><a href="#ref3">3</a></sup>
|[YesPlayerMusic](https://github.com/qier222/YesPlayMusic)|完全支持（请使用最新版）
---

1. <span id="ref1">由于热词对这些播放器的歌词匹配基于歌名歌手搜索，所以匹配可能不精准或匹配不到。</span>
2. <span id="ref2">请使用 [UWP 不更新版](https://github.com/JasonWei512/NetEase-Cloud-Music-UWP-Repack)，微软商店最新版为 Win32 版，非 UWP 版。</span>
3. <span id="ref3">由于播放器未提供进度信息，热词使用内置定时器更新歌词进度，所以当手动修改播放进度后热词将无法匹配到正确的歌词。</span>
4. <span id="ref4">可能无法获取到QQ音乐UWP的播放信息，先开启QQ音乐UWP再启动热词可以缓解。</span>

## 已知问题
* Windows 10 中关闭所有播放器时热词可能不会自动隐藏。
* 安装 StartAllBack 后热词渲染可能不正确，具体表现如歌词文字重叠，自定义颜色控件背景丢失等，卸载 StartAllBack 即可解决。
* 11 代 Intel 核心显卡使用 WDDM 版本为 2.7 的显卡驱动时，热词可能渲染不正确，升级核显驱动可以解决此问题，或开启热词的强制软件渲染进行缓解。

## 如何打开

### HyPlayer
安装热词后，在主界面中点击桌面歌词按钮。

![hyplayer](https://github.com/cnbluefire/HotLyric/blob/main/assets/hyplayer.png)

### LyricEase
安装热词后，在设置中启用桌面歌词选项。  

![lyricease](https://github.com/cnbluefire/HotLyric/blob/main/assets/lyricease.png)

### Spotify
Spotify 需要在设置中开启 **在使用媒体键时显示桌面重叠**
![spotify](https://github.com/cnbluefire/HotLyric/blob/main/assets/spotify.png)

## 使用说明

### 锁定歌词

选中后歌词界面将无法点击和拖拽。在 **通知区域图标上右键** 或 **设置界面中** 或 **双击通知区域图标** 即可解除锁定。

### 性能设置

如果感觉热词占用资源过多，可以打开 **低帧率模式** 和关闭 **边缘淡出** 以降低CPU占用。

如果歌词界面渲染错误，如文字重叠等，可以尝试打开 **强制使用软件渲染**。如果无法解决可以提Issues或在设置界面中点击 **反馈问题** 按钮联系我。

### 歌词样式

如果不喜欢预设的歌词样式，可以在右侧切换到自定义模式，调整界面上的每种颜色。
![custom-theme](https://github.com/cnbluefire/HotLyric/blob/main/assets/custom-theme.png)

### 开机启动
当在任务管理器中 **启用** 了热词的开机启动项时，应用内的开机启动选项才能设置。

### 重置窗口位置
如果窗口被拖动到显示器区域外，可以使用此选项恢复窗口的默认位置。
![reset-window-location](https://github.com/cnbluefire/HotLyric/blob/main/assets/reset-window-location.png)

## 第三方通知
[第三方通知](https://github.com/cnbluefire/HotLyric/blob/main/HotLyric/HotLyric.Package/ThirdPartyNotices.txt)
