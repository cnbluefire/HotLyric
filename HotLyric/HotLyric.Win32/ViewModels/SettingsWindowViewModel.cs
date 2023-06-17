using HotLyric.Win32.Controls;
using HotLyric.Win32.Models;
using HotLyric.Win32.Utils;
using HotLyric.Win32.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using Windows.ApplicationModel;
using Windows.Devices.Enumeration;
using Windows.Services.Store;
using Windows.Storage;
using Windows.System;
using Windows.UI.Xaml;
using Microsoft.UI.Xaml;
using HotLyric.Win32.Controls.LyricControlDrawingData;
using Newtonsoft.Json.Linq;
using WinRT;
using WinUIEx;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

namespace HotLyric.Win32.ViewModels
{
    public class SettingsWindowViewModel : ObservableObject
    {
        private const string KaraokeEnabledSettingKey = "Settings_KaraokeEnabled";
        private const string LyricHorizontalAlignmentSettingKey = "Settings_LyricHorizontalAlignment";
        private const string AlwaysShowBackgroundSettingKey = "Settings_AlwaysShowBackground";
        private const string ShowShadowSettingKey = "Settings_ShowShadow";
        private const string TextStrokeEnabledSettingKey = "Settings_TextStrokeEnabled";
        private const string TextShadowEnabledSettingKey = "Settings_TextShadowEnabled";
        private const string TextStrokeTypeSettingKey = "Settings_TextStrokeType";
        private const string LyricFontFamilySettingKey = "Settings_LyricFontFamily";
        private const string LyricFontStyleSettingKey = "Settings_LyricFontStyle";
        private const string LyricFontWeightSettingKey = "Settings_LyricFontWeight";
        private const string SecondRowSettingKey = "Settings_SecondRow";
        private const string SkipEmptyLyricLineSettingKey = "Settings_SkipEmptyLyricLine";
        private const string TextOpacityMaskSettingKey = "Settings_TextOpacityMask";
        private const string HideWhenFullScreenAppOpenSettingKey = "Settings_HideWhenFullScreenAppOpen";
        private const string WindowTransparentSettingKey = "Settings_WindowTransparent";
        private const string KeepWindowTransparentSettingKey = "Settings_KeepWindowTransparent";
        private const string ScrollAnimationModeKey = "Settings_ScrollAnimationMode";
        private const string LowFrameRateModeSettingKeyOld = "Settings_LowFrameRateMode";
        private const string LowFrameRateModeSettingKey = "Settings_LowFrameRateMode2";
        private const string LyricOpacitySettingKey = "Settings_LyricOpacity";
        private const string ShowLauncherWindowOnStartupSettingKey = "Settings_ShowLauncherWindowOnStartup";
        private const string HideOnPausedSettingKey = "Settings_HideOnPaused";
        private const string AutoResetWindowPosSettingsKey = "Settings_AutoResetWindowPos";
        private const string ReadMeAlreadyShowedOnStartUpSettingsKey = "Settings_ReadMeAlreadyShowedOnStartUp";
        private const string IsHotKeyEnabledSettingsKey = "Settings_IsHotKeyEnabled";

        public SettingsWindowViewModel()
        {
            keepWindowTransparent = LoadSetting(KeepWindowTransparentSettingKey, false);
            if (keepWindowTransparent)
            {
                windowTransparent = LoadSetting(WindowTransparentSettingKey, false);
            }

            var secondRowType = LoadSetting(SecondRowSettingKey, SecondRowType.TranslationOrNextLyric);
            SecondRowTypes.SelectedValue = secondRowType;

            karaokeEnabled = LoadSetting(KaraokeEnabledSettingKey, true);
            LyricAlignments.SelectedValue = LoadSetting(LyricHorizontalAlignmentSettingKey, LyricDrawingLineAlignment.Center);

            alwaysShowBackground = LoadSetting(AlwaysShowBackgroundSettingKey, false);

            var textStrokeType = LoadSetting<LyricControlTextStrokeType?>(TextStrokeTypeSettingKey, default);
            if (!textStrokeType.HasValue)
            {
                var textStrokeEnabled = LoadSetting(TextStrokeEnabledSettingKey, true);
                textStrokeType = textStrokeEnabled ? LyricControlTextStrokeType.Auto : LyricControlTextStrokeType.Disabled;
            }
            TextStrokeTypes.SelectedValue = textStrokeType.Value;

            textShadowEnabled = LoadSetting(TextShadowEnabledSettingKey, true);

            currentTheme = LyricThemeManager.CurrentThemeView ?? LyricThemeManager.LyricThemes[0];
            AllPresetThemes = LyricThemeManager.LyricThemes.ToArray();

            themeIsPresetVisible = currentTheme.Name != "customize";
            customizeTheme = currentTheme;

            allFontFamilies = FontFamilyDisplayModel.AllFamilies;

            lyricFontFamilySource = LoadSetting(LyricFontFamilySettingKey, "");
            lyricFontFamily = allFontFamilies.FirstOrDefault(c => c.Source == lyricFontFamilySource);

            if (lyricFontFamily == null)
            {
                lyricFontFamily = allFontFamilies[0];
                lyricFontFamilySource = lyricFontFamily.Source;
            }

            isLyricFontItalicStyleEnabled = LoadSetting(LyricFontStyleSettingKey, Windows.UI.Text.FontStyle.Normal) != Windows.UI.Text.FontStyle.Normal;
            isLyricFontBoldWeightEnabled = LoadSetting(LyricFontWeightSettingKey, Microsoft.UI.Text.FontWeights.Normal) != Microsoft.UI.Text.FontWeights.Normal;

            skipEmptyLyricLine = LoadSetting(SkipEmptyLyricLineSettingKey, true);
            textOpacityMask = LoadSetting(TextOpacityMaskSettingKey, !DeviceHelper.IsLowPerformanceDevice);
            hideWhenFullScreenAppOpen = LoadSetting(HideWhenFullScreenAppOpenSettingKey, true);

            ScrollAnimationMode.SelectedValue = LoadSetting(ScrollAnimationModeKey, LyricControlScrollAnimationMode.Slow);

            var lowFrameRateMode = LoadSetting(LowFrameRateModeSettingKey, (LowFrameRateMode?)null);
            var oldLowFrameRateMode = LoadSetting(LowFrameRateModeSettingKeyOld, (bool?)null);

            if (lowFrameRateMode.HasValue)
            {
                LowFrameRateMode.SelectedValue = lowFrameRateMode.Value;
            }
            else
            {
                LowFrameRateMode.SelectedValue = ViewModels.LowFrameRateMode.Auto;
                if (oldLowFrameRateMode.HasValue)
                {
                    LowFrameRateMode.SelectedValue = oldLowFrameRateMode.Value ? ViewModels.LowFrameRateMode.Enabled : ViewModels.LowFrameRateMode.Disabled;
                }
                else if (DeviceHelper.IsLowPerformanceDevice)
                {
                    LowFrameRateMode.SelectedValue = ViewModels.LowFrameRateMode.Disabled;
                }
            }

            lyricOpacity = Math.Min(Math.Max(LoadSetting(LyricOpacitySettingKey, 1d), 0.1d), 1d);

            showLauncherWindowOnStartup = LoadSetting(ShowLauncherWindowOnStartupSettingKey, true);

            StartupTaskHelper = new StartupTaskHelper("HotLyricStartupTask");
            StartupTaskHelper.PropertyChanged += StartupTaskHelper_PropertyChanged;

            hideOnPaused = LoadSetting(HideOnPausedSettingKey, false);

            autoResetWindowPos = LoadSetting(AutoResetWindowPosSettingsKey, true);

            hotKeyManager = new HotKeyManager(this);
            isHotKeyEnabled = LoadSetting(IsHotKeyEnabledSettingsKey, true);
        }

        private bool windowTransparent;
        private bool keepWindowTransparent;
        private EnumBindingModel<SecondRowType>? secondRowTypes;
        private bool karaokeEnabled;

        private EnumBindingModel<LyricDrawingLineAlignment>? lyricAlignments;

        private bool alwaysShowBackground;
        private EnumBindingModel<LyricControlTextStrokeType>? textStrokeTypes;
        private bool textShadowEnabled;
        private LyricThemeView currentTheme;
        private LyricThemeView customizeTheme;
        private bool themeIsPresetVisible;
        private string? lyricFontFamilySource;
        private FontFamilyDisplayModel? lyricFontFamily;
        private IReadOnlyList<FontFamilyDisplayModel> allFontFamilies;
        private bool isLyricFontItalicStyleEnabled;
        private bool isLyricFontBoldWeightEnabled;
        private AsyncRelayCommand? clearCacheCmd;
        private AsyncRelayCommand? openStorePageCmd;
        private AsyncRelayCommand? feedbackCmd;
        private AsyncRelayCommand? checkUpdateCmd;
        private AsyncRelayCommand? thirdPartyNoticeCmd;
        private AsyncRelayCommand? githubCmd;
        private AsyncRelayCommand? fontSizeCmd;
        private bool skipEmptyLyricLine;
        private bool textOpacityMask;
        private bool hideWhenFullScreenAppOpen;
        private EnumBindingModel<LyricControlScrollAnimationMode>? scrollAnimationMode;
        private EnumBindingModel<LowFrameRateMode>? lowFrameRateMode;
        private double lyricOpacity;
        private bool showLauncherWindowOnStartup;
        private bool hideOnPaused;
        private bool autoResetWindowPos;
        private AsyncRelayCommand? spotifySetLanguage;
        private HotKeyManager hotKeyManager;
        private bool isHotKeyEnabled;

        public StartupTaskHelper StartupTaskHelper { get; }

        public bool WindowTransparent
        {
            get => windowTransparent;
            set => ChangeSettings(ref windowTransparent, value, WindowTransparentSettingKey);
        }

        public bool KeepWindowTransparent
        {
            get => keepWindowTransparent;
            set => ChangeSettings(ref keepWindowTransparent, value, KeepWindowTransparentSettingKey);
        }

        public EnumBindingModel<SecondRowType> SecondRowTypes => secondRowTypes ?? (secondRowTypes = new EnumBindingModel<SecondRowType>(
            new[]
            {
                new EnumDisplayModel<SecondRowType>("显示翻译或下一行歌词", SecondRowType.TranslationOrNextLyric),
                new EnumDisplayModel<SecondRowType>("仅显示下一行歌词", SecondRowType.NextLyricOnly),
                new EnumDisplayModel<SecondRowType>("隐藏", SecondRowType.Collapsed),
            }, value =>
            {
                if (value.HasValue)
                {
                    this.SetSettingsAndNotify(SecondRowSettingKey, value.Value);
                }
            }));

        public bool KaraokeEnabled
        {
            get => karaokeEnabled;
            set => ChangeSettings(ref karaokeEnabled, value, KaraokeEnabledSettingKey);
        }

        public EnumBindingModel<LyricDrawingLineAlignment> LyricAlignments => lyricAlignments ?? (lyricAlignments = new EnumBindingModel<LyricDrawingLineAlignment>(
            new[]
            {
                new EnumDisplayModel<LyricDrawingLineAlignment>("左对齐", LyricDrawingLineAlignment.Left),
                new EnumDisplayModel<LyricDrawingLineAlignment>("居中", LyricDrawingLineAlignment.Center),
                new EnumDisplayModel<LyricDrawingLineAlignment>("右对齐", LyricDrawingLineAlignment.Right),
            }, value =>
            {
                if (value.HasValue)
                {
                    this.SetSettingsAndNotify(LyricHorizontalAlignmentSettingKey, value.Value);
                }
            }));

        public bool AlwaysShowBackground
        {
            get => alwaysShowBackground;
            set => ChangeSettings(ref alwaysShowBackground, value, AlwaysShowBackgroundSettingKey);
        }

        public EnumBindingModel<LyricControlTextStrokeType> TextStrokeTypes => textStrokeTypes ?? (textStrokeTypes = new EnumBindingModel<LyricControlTextStrokeType>(
            new[]
            {
                new EnumDisplayModel<LyricControlTextStrokeType>("自动", LyricControlTextStrokeType.Auto),
                new EnumDisplayModel<LyricControlTextStrokeType>("描边", LyricControlTextStrokeType.Enabled),
                new EnumDisplayModel<LyricControlTextStrokeType>("禁用", LyricControlTextStrokeType.Disabled),
            }, value =>
            {
                if (value.HasValue)
                {
                    this.SetSettingsAndNotify(TextStrokeTypeSettingKey, value.Value);
                }
            }));

        public bool TextShadowEnabled
        {
            get => textShadowEnabled;
            set => ChangeSettings(ref textShadowEnabled, value, TextShadowEnabledSettingKey);
        }

        public LyricThemeView[] AllPresetThemes { get; }

        public LyricThemeView CurrentTheme
        {
            get => currentTheme;
            set
            {
                if (ChangeSettings(ref currentTheme, value))
                {
                    LyricThemeManager.CurrentThemeView = value;
                    customizeTheme = value;
                    OnPropertyChanged(nameof(CustomizeTheme));
                    ThemeIsPresetVisible = currentTheme.Name != "customize";
                }
            }
        }

        public LyricThemeView CustomizeTheme
        {
            get => customizeTheme;
            set
            {
                if (SetProperty(ref customizeTheme, value))
                {
                    LyricThemeManager.CurrentThemeView = value;
                    CurrentTheme = value ?? LyricThemeManager.LyricThemes[0];
                }
            }
        }

        public bool ThemeIsPresetVisible
        {
            get => themeIsPresetVisible;
            set => SetProperty(ref themeIsPresetVisible, value);
        }

        public IReadOnlyList<FontFamilyDisplayModel> AllFontFamilies => allFontFamilies.ToArray();

        public FontFamilyDisplayModel? LyricFontFamily
        {
            get => lyricFontFamily;
            set
            {
                if (SetProperty(ref lyricFontFamily, value))
                {
                    ChangeSettings(ref lyricFontFamilySource, value?.Source ?? "", LyricFontFamilySettingKey);
                }
            }
        }


        public bool IsLyricFontItalicStyleEnabled
        {
            get => isLyricFontItalicStyleEnabled;
            set
            {
                if (SetProperty(ref isLyricFontItalicStyleEnabled, value))
                {
                    SetSettingsAndNotify(LyricFontStyleSettingKey, value ? Windows.UI.Text.FontStyle.Italic : Windows.UI.Text.FontStyle.Normal);
                }
            }
        }

        public bool IsLyricFontBoldWeightEnabled
        {
            get => isLyricFontBoldWeightEnabled;
            set
            {
                if (SetProperty(ref isLyricFontBoldWeightEnabled, value))
                {
                    SetSettingsAndNotify(LyricFontWeightSettingKey, value ? Microsoft.UI.Text.FontWeights.Bold : Microsoft.UI.Text.FontWeights.Normal);
                }
            }
        }

        public bool SkipEmptyLyricLine
        {
            get => skipEmptyLyricLine;
            set => ChangeSettings(ref skipEmptyLyricLine, value, SkipEmptyLyricLineSettingKey);
        }

        public bool TextOpacityMask
        {
            get => textOpacityMask;
            set => ChangeSettings(ref textOpacityMask, value, TextOpacityMaskSettingKey);
        }

        public bool HideWhenFullScreenAppOpen
        {
            get => hideWhenFullScreenAppOpen;
            set => ChangeSettings(ref hideWhenFullScreenAppOpen, value, HideWhenFullScreenAppOpenSettingKey);
        }

        public EnumBindingModel<LyricControlScrollAnimationMode> ScrollAnimationMode => scrollAnimationMode ?? (scrollAnimationMode = new EnumBindingModel<LyricControlScrollAnimationMode>(
            new[]
            {
                new EnumDisplayModel<LyricControlScrollAnimationMode>("快速", LyricControlScrollAnimationMode.Fast),
                new EnumDisplayModel<LyricControlScrollAnimationMode>("慢速", LyricControlScrollAnimationMode.Slow),
                new EnumDisplayModel<LyricControlScrollAnimationMode>("禁用", LyricControlScrollAnimationMode.Disabled),
            }, value =>
            {
                if (value.HasValue)
                {
                    this.SetSettingsAndNotify(ScrollAnimationModeKey, value.Value);
                }
            }));

        public EnumBindingModel<LowFrameRateMode> LowFrameRateMode => lowFrameRateMode ?? (lowFrameRateMode = new EnumBindingModel<LowFrameRateMode>(
            new[]
            {
                new EnumDisplayModel<LowFrameRateMode>("自动", ViewModels.LowFrameRateMode.Auto),
                new EnumDisplayModel<LowFrameRateMode>("启用", ViewModels.LowFrameRateMode.Enabled),
                new EnumDisplayModel<LowFrameRateMode>("禁用", ViewModels.LowFrameRateMode.Disabled),
            }, value =>
            {
                if (value.HasValue)
                {
                    this.SetSettingsAndNotify(LowFrameRateModeSettingKey, value.Value);
                }
            }));

        public bool HideOnPaused
        {
            get => hideOnPaused;
            set => ChangeSettings(ref hideOnPaused, value, HideOnPausedSettingKey);
        }

        public bool AutoResetWindowPos
        {
            get => autoResetWindowPos;
            set => ChangeSettings(ref autoResetWindowPos, value, AutoResetWindowPosSettingsKey);
        }

        public double LyricOpacity
        {
            get => lyricOpacity;
            set => ChangeSettings(ref lyricOpacity, value, LyricOpacitySettingKey);
        }

        public bool ShowLauncherWindowOnStartup
        {
            get => showLauncherWindowOnStartup;
            set => ChangeSettings(ref showLauncherWindowOnStartup, value, ShowLauncherWindowOnStartupSettingKey);
        }

        public bool ShowLauncherWindowOnStartupEnabled => !StartupTaskHelper.IsStartupTaskEnabled;

        private void StartupTaskHelper_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (!ShowLauncherWindowOnStartupEnabled)
            {
                ShowLauncherWindowOnStartup = false;
            }
            OnPropertyChanged(nameof(ShowLauncherWindowOnStartupEnabled));
        }

        public string AppName => Package.Current.DisplayName;

        public string Author => Package.Current.PublisherDisplayName;

        public string AppVersion
        {
            get
            {
                var v = Package.Current.Id.Version;
                return new Version(v.Major, v.Minor, v.Build, v.Revision).ToString();
            }
        }

        public AsyncRelayCommand ClearCacheCmd => clearCacheCmd ?? (clearCacheCmd = new AsyncRelayCommand(async () =>
        {
            try
            {
                await LrcHelper.ClearCacheAsync();
            }
            catch (Exception ex)
            {
                HotLyric.Win32.Utils.LogHelper.LogError(ex);
            }
        }, () => !ClearCacheCmd.IsRunning));

        public AsyncRelayCommand OpenStorePageCmd => openStorePageCmd ?? (openStorePageCmd = new AsyncRelayCommand(async () =>
        {
            try
            {
                var uri = new Uri("ms-windows-store://pdp/?productid=9MXFFHVQVBV9");
                var fallbackUri = new Uri("https://www.microsoft.com/store/apps/9MXFFHVQVBV9");

                await Launcher.LaunchUriAsync(uri, new LauncherOptions()
                {
                    FallbackUri = fallbackUri,
                });
            }
            catch (Exception ex)
            {
                HotLyric.Win32.Utils.LogHelper.LogError(ex);
            }
        }));

        public AsyncRelayCommand FeedbackCmd => feedbackCmd ?? (feedbackCmd = new AsyncRelayCommand(async () =>
        {
            try
            {
                //var dict = new Dictionary<string, string>()
                //{
                //    ["subject"] = $"热词 v{AppVersion} 问题反馈"
                //};

                //var query = string.Join('&', dict.Select(c => $"{Uri.EscapeDataString(c.Key)}={Uri.EscapeDataString(c.Value)}"));

                //await Launcher.LaunchUriAsync(new Uri($"mailto://blue-fire@outlook.com?{query}"));

                await Launcher.LaunchUriAsync(new Uri("https://jq.qq.com/?_wv=1027&k=K4Ixe2Gw"));
            }
            catch (Exception ex)
            {
                HotLyric.Win32.Utils.LogHelper.LogError(ex);
            }
        }));

        public AsyncRelayCommand CheckUpdateCmd => checkUpdateCmd ?? (checkUpdateCmd = new AsyncRelayCommand(async () =>
        {
            try
            {
                var ownerWindow = App.Current.SettingsView;
                if (ownerWindow?.Visible != true) return;

                var updateResult = await ApplicationHelper.CheckUpdateAsync();

                ownerWindow = App.Current.SettingsView;
                if (ownerWindow?.Visible != true) return;

                if (updateResult.HasUpdate)
                {
                    var contentDialog = new ContentDialog()
                    {
                        Title = "热词",
                        Content = "发现新版本，是否跳转到商店下载？",
                        Margin = new Thickness(0, 32, 0, 12),
                        IsPrimaryButtonEnabled = true,
                        IsSecondaryButtonEnabled = false,
                        PrimaryButtonText = "是",
                        CloseButtonText = "否",
                        CornerRadius = new CornerRadius(8),
                        XamlRoot = ownerWindow.Content.XamlRoot
                    };

                    var result = await contentDialog.ShowAsync();
                    if (result == ContentDialogResult.Primary)
                    {
                        _ = updateResult.TryStartUpdateAsync();
                        OpenStorePageCmd.Execute(null);
                    }
                }
                else
                {
                    var contentDialog = new ContentDialog()
                    {
                        Title = "热词",
                        Content = "未发现新版本",
                        Margin = new Thickness(0, 32, 0, 12),
                        IsPrimaryButtonEnabled = true,
                        IsSecondaryButtonEnabled = false,
                        PrimaryButtonText = "确定",
                        CornerRadius = new CornerRadius(8),
                        XamlRoot = ownerWindow.Content.XamlRoot
                    };

                    await contentDialog.ShowAsync();
                }
            }
            catch (Exception ex)
            {
                HotLyric.Win32.Utils.LogHelper.LogError(ex);
            }

        }, () => !CheckUpdateCmd.IsRunning));

        public AsyncRelayCommand ThirdPartyNoticeCmd => thirdPartyNoticeCmd ?? (thirdPartyNoticeCmd = new AsyncRelayCommand(async () =>
        {
            try
            {
                var path = System.IO.Path.Combine(new System.IO.DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory).Parent!.FullName, "ThirdPartyNotices.txt");
                if (System.IO.File.Exists(path))
                {
                    var allText = await System.IO.File.ReadAllTextAsync(path);
                    if (!string.IsNullOrEmpty(allText))
                    {
                        var ownerWindow = App.Current.SettingsView;

                        if (ownerWindow?.Visible != true) return;

                        var contentDialog = new ContentDialog()
                        {
                            Title = "第三方通知",
                            Content = new ScrollViewer()
                            {
                                Content = new TextBlock()
                                {
                                    Text = allText,
                                    TextWrapping = TextWrapping.Wrap,
                                    FontSize = 12
                                }
                            },
                            Margin = new Thickness(0, 32, 0, 12),
                            IsPrimaryButtonEnabled = true,
                            IsSecondaryButtonEnabled = false,
                            PrimaryButtonText = "确定",
                            CornerRadius = new CornerRadius(8),
                            XamlRoot = ownerWindow.Content.XamlRoot
                        };

                        await contentDialog.ShowAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                HotLyric.Win32.Utils.LogHelper.LogError(ex);
            }
        }, () => !ThirdPartyNoticeCmd.IsRunning));

        public AsyncRelayCommand GithubCmd => githubCmd ?? (githubCmd = new AsyncRelayCommand(async () =>
        {
            var uri = new Uri("https://github.com/cnbluefire/HotLyric");
            await Launcher.LaunchUriAsync(uri);
        }));

        public AsyncRelayCommand FontSizeCmd => fontSizeCmd ?? (fontSizeCmd = new AsyncRelayCommand(async () =>
        {
            var ownerWindow = App.Current.SettingsView;

            if (ownerWindow?.Visible != true) return;

            var contentDialog = new ContentDialog()
            {
                Title = "字号帮助",
                Content = new TextBlock()
                {
                    Text = "拖拽歌词窗口尺寸即可改变文字大小",
                    TextWrapping = TextWrapping.Wrap,
                    FontSize = 14
                },
                Margin = new Thickness(0, 32, 0, 12),
                IsPrimaryButtonEnabled = true,
                IsSecondaryButtonEnabled = true,
                PrimaryButtonText = "查看更多帮助",
                SecondaryButtonText = "确定",
                CornerRadius = new CornerRadius(8),
            };

            contentDialog.XamlRoot = ownerWindow.Content.XamlRoot;
            var res = await contentDialog.ShowAsync();

            if (res == ContentDialogResult.Primary)
            {
                ShowReadMe();
            }
        }));

        public AsyncRelayCommand SpotifySetLanguage => spotifySetLanguage ?? (spotifySetLanguage = new AsyncRelayCommand(async () =>
        {
            SpotifySetLanguage.NotifyCanExecuteChanged();

            List<string> prefFileList = new List<string>();

            try
            {
                var storePackageFolder = await ApplicationHelper.TryGetPackageFromAppUserModelIdAsync("SpotifyAB.SpotifyMusic_zpdnekdrzrea0");
                if (storePackageFolder != null)
                {
                    var folder = ApplicationHelper.GetAppDataFolderLocation(storePackageFolder);

                    if (!string.IsNullOrEmpty(folder))
                    {
                        var filePath = System.IO.Path.Combine(folder, "LocalState", "Spotify", "prefs");
                        if (System.IO.File.Exists(filePath))
                        {
                            prefFileList.Add(filePath);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                HotLyric.Win32.Utils.LogHelper.LogError(ex);
            }

            try
            {
                var filePath = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "Spotify", "prefs");

                if (System.IO.File.Exists(filePath))
                {
                    prefFileList.Add(filePath);
                }
            }
            catch (Exception ex)
            {
                HotLyric.Win32.Utils.LogHelper.LogError(ex);
            }

            var regex = new Regex("language=\".+?\"");

            int failedCount = 0;

            foreach (var file in prefFileList)
            {
                try
                {
                    using (var fileStream = new FileStream(file, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
                    {
                        string content = "";
                        using (var reader = new StreamReader(fileStream, Encoding.UTF8, true, 1024, leaveOpen: true))
                        {
                            content = await reader.ReadToEndAsync();
                        }

                        bool flag = false;
                        if (regex.IsMatch(content))
                        {
                            var content2 = regex.Replace(content, m => "language=\"zh-CN\"");
                            flag = content != content2;
                            content = content2;
                        }
                        else
                        {
                            flag = true;

                            var sb = new StringBuilder(content);
                            if (!content.EndsWith("\n"))
                            {
                                sb.Append('\n');
                            }
                            sb.Append("language=\"zh-CN\"\n");
                            content = sb.ToString();
                        }

                        if (flag)
                        {
                            fileStream.Seek(0, SeekOrigin.Begin);
                            using (var writer = new StreamWriter(fileStream, Encoding.UTF8, 1024, leaveOpen: true))
                            {
                                await writer.WriteAsync(content);
                                await writer.FlushAsync();
                            }

                            fileStream.SetLength(fileStream.Position);
                        }
                    }
                }
                catch
                {
                    failedCount++;
                }
            }

            var ownerWindow = App.Current.SettingsView;
            if (ownerWindow?.Visible == true)
            {
                var contentDialog = new ContentDialog()
                {
                    Title = "热词",
                    Margin = new Thickness(0, 32, 0, 12),
                    IsPrimaryButtonEnabled = false,
                    IsSecondaryButtonEnabled = false,
                    CloseButtonText = "确定",
                    CornerRadius = new CornerRadius(8),
                    XamlRoot = ownerWindow.Content.XamlRoot
                };


                if (failedCount == prefFileList.Count)
                {
                    if (prefFileList.Count == 0)
                    {
                        contentDialog.Content = "设置失败。未找到Spotify配置文件。";
                    }
                    else
                    {
                        contentDialog.Content = "设置失败。";
                    }
                }
                else
                {
                    contentDialog.Content = "设置成功，请手动重启Spotify。";
                }

                await contentDialog.ShowAsync();
            }

        }, () => !SpotifySetLanguage.IsRunning));


        public HotKeyManager HotKeyManager => hotKeyManager;

        public bool IsHotKeyEnabled
        {
            get => isHotKeyEnabled;
            set
            {
                if (ChangeSettings(ref isHotKeyEnabled, value, IsHotKeyEnabledSettingsKey))
                {
                    UpdateHotKeyManagerState();
                }
            }
        }

        [return: MaybeNull]
        internal T LoadSetting<T>(string? settingsKey, [AllowNull] T defaultValue = default)
        {
            settingsKey = settingsKey?.Trim();
            if (string.IsNullOrEmpty(settingsKey)) return defaultValue;

            try
            {
                if (ApplicationData.Current.LocalSettings.Values.TryGetValue(settingsKey, out var _json)
                    && _json is string json)
                {
                    return JsonConvert.DeserializeObject<T>(json);
                }
            }
            catch (Exception ex)
            {
                HotLyric.Win32.Utils.LogHelper.LogError(ex);
            }
            return defaultValue;
        }

        private bool ChangeSettings<T>(ref T field, T value, string? settingsKey = null, [CallerMemberName] string propertyName = "")
        {
            if (SetProperty(ref field, value, propertyName))
            {
                SetSettingsAndNotify(settingsKey, value);

                return true;
            }
            return false;
        }

        internal void SetSettings<T>(string? settingsKey, T value)
        {
            settingsKey = settingsKey?.Trim();
            if (!string.IsNullOrEmpty(settingsKey))
            {
                try
                {
                    if (value is null)
                    {
                        ApplicationData.Current.LocalSettings.Values.Remove(settingsKey);
                    }
                    else
                    {
                        var json = JsonConvert.SerializeObject(value);
                        ApplicationData.Current.LocalSettings.Values[settingsKey] = json;
                    }
                }
                catch (Exception ex)
                {
                    HotLyric.Win32.Utils.LogHelper.LogError(ex);
                }
            }
        }

        private void SetSettingsAndNotify<T>(string? settingsKey, T value)
        {
            SetSettings(settingsKey, value);

            try
            {
                NotifySettingsChanged();
            }
            catch (Exception ex)
            {
                HotLyric.Win32.Utils.LogHelper.LogError(ex);
            }
        }

        private void NotifySettingsChanged()
        {
            SettingsChanged?.Invoke(this, EventArgs.Empty);
        }

        public event EventHandler? SettingsChanged;

        public void ShowSettingsWindow()
        {
            if (App.Current.SettingsView == null)
            {
                App.Current.SettingsView = new();
            }

            try
            {
                App.Current.SettingsView.Activate();
                App.Current.SettingsView.SetForegroundWindow();
            }
            catch (Exception ex)
            {
                HotLyric.Win32.Utils.LogHelper.LogError(ex);
            }
        }

        //public void ShowLauncherWindow()
        //{
        //    var launcherWindow = App.Current.Windows.OfType<LauncherWindow>().FirstOrDefault();
        //    if (launcherWindow == null)
        //    {
        //        launcherWindow = new LauncherWindow();
        //    }

        //    try
        //    {
        //        launcherWindow.Show();
        //        launcherWindow.Activate();
        //    }
        //    catch (Exception ex)
        //    {
        //        HotLyric.Win32.Utils.LogHelper.LogError(ex);
        //    }
        //}

        public void ShowReadMe()
        {
            App.DispatcherQueue.TryEnqueue(() =>
            {
                ShowSettingsWindow();

                App.Current.SettingsView?.NavigateToPage("ReadMe");
            });
        }

        public void TryShowReadMeOnStartup()
        {
            if (!LoadSetting(ReadMeAlreadyShowedOnStartUpSettingsKey, false))
            {
                SetSettings(ReadMeAlreadyShowedOnStartUpSettingsKey, true);
                ShowReadMe();
            }
        }

        public void ActivateInstance()
        {
            if (ViewModelLocator.Instance.LyricWindowViewModel.SelectedSession != null
                || ActivationArgumentsHelper.HasLaunchParameters)
            {
                if (ViewModelLocator.Instance.LyricWindowViewModel.IsMinimized)
                {
                    ViewModelLocator.Instance.LyricWindowViewModel.IsMinimized = false;
                }
                else
                {
                    ViewModelLocator.Instance.LyricWindowViewModel.ShowBackgroundTransient(TimeSpan.FromSeconds(2));
                }
            }
        }

        public void UpdateHotKeyManagerState()
        {
            bool install = false;

            if (!App.Current.Exiting && IsHotKeyEnabled)
            {
                var activated = IsActivated(App.Current.SettingsView);
                if (activated)
                {
                    install = FocusManager.GetFocusedElement(App.Current.SettingsView!.Content.XamlRoot) is not HotKeyInputBox;
                }
                else
                {
                    install = true;
                }
            }

            if (install)
            {
                HotKeyManager.Install();
            }
            else
            {
                HotKeyManager.Uninstall();
            }

            static bool IsActivated(Microsoft.UI.Xaml.Window? _window) =>
                _window != null
                    && _window.Visible
                    && Vanara.PInvoke.User32.GetForegroundWindow().DangerousGetHandle().ToInt64() == (long)_window.AppWindow.Id.Value;
        }
    }

    public enum LowFrameRateMode
    {
        Auto,
        Enabled,
        Disabled
    }
}
