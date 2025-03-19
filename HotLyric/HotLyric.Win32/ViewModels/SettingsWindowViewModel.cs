using BlueFire.Toolkit.WinUI3.Extensions;
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
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI;
using Vanara.PInvoke;
using BlueFire.Toolkit.WinUI3.Input;
using System.Threading.Tasks;

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
        private const string LyricWesternTextFontFamilySettingKey = "Settings_LyricWesternTextFontFamily";
        private const string LyricJapaneseKanaFontFamilySettingKey = "Settings_LyricJapaneseKanaFontFamily";
        private const string LyricKoreanHangulFontFamilySettingKey = "Settings_LyricKoreanHangulFontFamily";
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
        private const string HttpClientProxySettingKey = "Settings_HttpClientProxy";
        private const string IsLogEnableSettingKey = "Settings_IsLogEnable";

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

            var _allFontFamiliesWithEmpty = new List<FontFamilyDisplayModel>()
            {
                FontFamilyDisplayModel.EmptyModel
            };
            _allFontFamiliesWithEmpty.AddRange(FontFamilyDisplayModel.AllFamilies);

            allFontFamiliesWithEmpty = _allFontFamiliesWithEmpty;

            lyricFontFamilySource = LoadSetting(LyricFontFamilySettingKey, "");
            lyricFontFamily = allFontFamilies.FirstOrDefault(c => c.Source == lyricFontFamilySource);

            if (lyricFontFamily == null)
            {
                lyricFontFamily = allFontFamilies[0];
                lyricFontFamilySource = lyricFontFamily.Source;
            }

            lyricWesternTextFontFamilySource = LoadSetting(LyricWesternTextFontFamilySettingKey, "");
            lyricWesternTextFontFamily = allFontFamiliesWithEmpty.FirstOrDefault(c => c.Source == lyricWesternTextFontFamilySource);

            if (lyricWesternTextFontFamily == null)
            {
                lyricWesternTextFontFamily = FontFamilyDisplayModel.EmptyModel;
                lyricWesternTextFontFamilySource = lyricWesternTextFontFamily.Source;
            }


            lyricJapaneseKanaFontFamilySource = LoadSetting(LyricJapaneseKanaFontFamilySettingKey, "");
            lyricJapaneseKanaFontFamily = allFontFamiliesWithEmpty.FirstOrDefault(c => c.Source == lyricJapaneseKanaFontFamilySource);

            if (lyricJapaneseKanaFontFamily == null)
            {
                lyricJapaneseKanaFontFamily = FontFamilyDisplayModel.EmptyModel;
                lyricJapaneseKanaFontFamilySource = lyricJapaneseKanaFontFamily.Source;
            }


            lyricKoreanHangulFontFamilySource = LoadSetting(LyricKoreanHangulFontFamilySettingKey, "");
            lyricKoreanHangulFontFamily = allFontFamiliesWithEmpty.FirstOrDefault(c => c.Source == lyricKoreanHangulFontFamilySource);

            if (lyricKoreanHangulFontFamily == null)
            {
                lyricKoreanHangulFontFamily = FontFamilyDisplayModel.EmptyModel;
                lyricKoreanHangulFontFamilySource = lyricKoreanHangulFontFamily.Source;
            }

            FontFamilySets.PrimaryFontFamily = !string.IsNullOrEmpty(lyricFontFamilySource) ? lyricFontFamilySource : "SYSTEM-UI";
            FontFamilySets.WesternTextFontFamily = lyricWesternTextFontFamilySource;
            FontFamilySets.JapaneseKanaFontFamily = lyricJapaneseKanaFontFamilySource;
            FontFamilySets.KoreanHangulFontFamily = lyricKoreanHangulFontFamilySource;

            lyricCompositedFontFamily = FontFamilySets.CompositedFontFamily;

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

            hotKeyModels = new HotKeyModels(this);
            isHotKeyEnabled = LoadSetting(IsHotKeyEnabledSettingsKey, true);
            HotKeyManager.IsEnabled = isHotKeyEnabled;

            httpClientProxy = LoadSetting<HttpClientProxyModel>(HttpClientProxySettingKey, null);
            HttpClientManager.Proxy = httpClientProxy?.CreateConfigure();

            LogHelper.IsLogEnabled = LoadSetting<bool>(IsLogEnableSettingKey, true);
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

        private string? lyricWesternTextFontFamilySource;
        private FontFamilyDisplayModel? lyricWesternTextFontFamily;

        private string? lyricJapaneseKanaFontFamilySource;
        private FontFamilyDisplayModel? lyricJapaneseKanaFontFamily;

        private string? lyricKoreanHangulFontFamilySource;
        private FontFamilyDisplayModel? lyricKoreanHangulFontFamily;

        private string lyricCompositedFontFamily = "";

        private IReadOnlyList<FontFamilyDisplayModel> allFontFamilies;
        private IReadOnlyList<FontFamilyDisplayModel> allFontFamiliesWithEmpty;
        private bool isLyricFontItalicStyleEnabled;
        private bool isLyricFontBoldWeightEnabled;
        private AsyncRelayCommand? clearCacheCmd;
        private AsyncRelayCommand? openStorePageCmd;
        private AsyncRelayCommand? feedbackCmd;
        private AsyncRelayCommand? checkUpdateCmd;
        private AsyncRelayCommand? openLogFolderCmd;
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
        private HotKeyModels hotKeyModels;
        private bool isHotKeyEnabled;
        private HttpClientProxyModel? httpClientProxy;
        private AsyncRelayCommand? changeProxyCmd;
        private AsyncRelayCommand? deleteLogFilesWhenLogDisabledCmd;

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

        public IReadOnlyList<FontFamilyDisplayModel> AllFontFamilies => allFontFamilies;

        public IReadOnlyList<FontFamilyDisplayModel> AllFontFamiliesWithEmpty => allFontFamiliesWithEmpty;

        public FontFamilyDisplayModel? LyricFontFamily
        {
            get => lyricFontFamily;
            set
            {
                if (SetProperty(ref lyricFontFamily, value))
                {
                    ChangeSettings(ref lyricFontFamilySource, value?.Source ?? "", LyricFontFamilySettingKey);
                    FontFamilySets.PrimaryFontFamily = !string.IsNullOrEmpty(lyricFontFamilySource) ? lyricFontFamilySource : "SYSTEM-UI";
                    LyricCompositedFontFamily = "";
                    LyricCompositedFontFamily = FontFamilySets.CompositedFontFamily;
                }
            }
        }

        public FontFamilyDisplayModel? LyricWesternTextFontFamily
        {
            get => lyricWesternTextFontFamily;
            set
            {
                if (SetProperty(ref lyricWesternTextFontFamily, value))
                {
                    ChangeSettings(ref lyricWesternTextFontFamilySource, value?.Source ?? "", LyricWesternTextFontFamilySettingKey);
                    FontFamilySets.WesternTextFontFamily = lyricWesternTextFontFamilySource;
                    LyricCompositedFontFamily = "";
                    LyricCompositedFontFamily = FontFamilySets.CompositedFontFamily;
                }
            }
        }

        public FontFamilyDisplayModel? LyricJapaneseKanaFontFamily
        {
            get => lyricJapaneseKanaFontFamily;
            set
            {
                if (SetProperty(ref lyricJapaneseKanaFontFamily, value))
                {
                    ChangeSettings(ref lyricJapaneseKanaFontFamilySource, value?.Source ?? "", LyricJapaneseKanaFontFamilySettingKey);
                    FontFamilySets.JapaneseKanaFontFamily = lyricJapaneseKanaFontFamilySource;
                    LyricCompositedFontFamily = "";
                    LyricCompositedFontFamily = FontFamilySets.CompositedFontFamily;
                }
            }
        }

        public FontFamilyDisplayModel? LyricKoreanHangulFontFamily
        {
            get => lyricKoreanHangulFontFamily;
            set
            {
                if (SetProperty(ref lyricKoreanHangulFontFamily, value))
                {
                    ChangeSettings(ref lyricKoreanHangulFontFamilySource, value?.Source ?? "", LyricKoreanHangulFontFamilySettingKey);
                    FontFamilySets.KoreanHangulFontFamily = lyricKoreanHangulFontFamilySource;
                    OnPropertyChanged(nameof(LyricCompositedFontFamily));
                }
            }
        }

        public string LyricCompositedFontFamily
        {
            get => lyricCompositedFontFamily;
            private set => SetProperty(ref lyricCompositedFontFamily, value);
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
                if (ownerWindow?.XamlWindow?.Visible != true) return;

                var updateResult = await ApplicationHelper.CheckUpdateAsync();

                ownerWindow = App.Current.SettingsView;
                if (ownerWindow?.XamlWindow?.Visible != true) return;

                if (updateResult.HasUpdate)
                {
                    var contentDialog = new ContentDialog()
                    {
                        Title = "热词",
                        Content = "发现新版本，是否跳转到商店下载？",
                        IsPrimaryButtonEnabled = true,
                        IsSecondaryButtonEnabled = false,
                        PrimaryButtonText = "是",
                        CloseButtonText = "否",
                        CornerRadius = new CornerRadius(8),
                        XamlRoot = ownerWindow.Content?.XamlRoot
                    };

                    var result = await contentDialog.ShowModalWindowAsync();
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
                        IsPrimaryButtonEnabled = true,
                        IsSecondaryButtonEnabled = false,
                        PrimaryButtonText = "确定",
                        CornerRadius = new CornerRadius(8),
                        XamlRoot = ownerWindow.Content?.XamlRoot
                    };

                    await contentDialog.ShowModalWindowAsync();
                }
            }
            catch (Exception ex)
            {
                HotLyric.Win32.Utils.LogHelper.LogError(ex);
            }

        }, () => !CheckUpdateCmd.IsRunning));

        public AsyncRelayCommand OpenLogFolderCmd => openLogFolderCmd ?? (openLogFolderCmd = new AsyncRelayCommand(async () =>
        {
            try
            {
                var logsFolderPath = System.IO.Path.Combine(
                        ApplicationData.Current.LocalCacheFolder.Path,
                        "Local",
                        "Logs");

                if (!Directory.Exists(logsFolderPath))
                {
                    Directory.CreateDirectory(logsFolderPath);
                }

                var folder = await StorageFolder.GetFolderFromPathAsync(logsFolderPath);

                await Launcher.LaunchFolderAsync(folder);
            }
            catch (Exception ex)
            {
                LogHelper.LogError(ex);
            }
        }));

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

                        if (ownerWindow?.XamlWindow?.Visible != true) return;

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
                                },
                                MaxWidth = 600,
                                MaxHeight = 600,
                            },
                            IsPrimaryButtonEnabled = true,
                            IsSecondaryButtonEnabled = false,
                            PrimaryButtonText = "确定",
                            CornerRadius = new CornerRadius(8),
                            XamlRoot = ownerWindow.Content?.XamlRoot
                        };

                        await contentDialog.ShowModalWindowAsync();
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

            if (ownerWindow?.XamlWindow?.Visible != true) return;

            var contentDialog = new ContentDialog()
            {
                Title = "字号帮助",
                Content = new TextBlock()
                {
                    Text = "拖拽歌词窗口尺寸即可改变文字大小",
                    TextWrapping = TextWrapping.Wrap,
                    FontSize = 14,
                },
                IsPrimaryButtonEnabled = true,
                IsSecondaryButtonEnabled = true,
                PrimaryButtonText = "查看更多帮助",
                SecondaryButtonText = "确定",
                CornerRadius = new CornerRadius(8),
            };

            contentDialog.XamlRoot = ownerWindow.Content?.XamlRoot;
            var res = await contentDialog.ShowModalWindowAsync();

            if (res == ContentDialogResult.Primary)
            {
                ShowReadMe();
            }
        }));


        public HotKeyModels HotKeyModels => hotKeyModels;

        public bool IsHotKeyEnabled
        {
            get => isHotKeyEnabled;
            set
            {
                if (ChangeSettings(ref isHotKeyEnabled, value, IsHotKeyEnabledSettingsKey))
                {
                    HotKeyManager.IsEnabled = value;
                }
            }
        }

        public bool IsLogEnabled
        {
            get => LogHelper.IsLogEnabled;
            set
            {
                if (LogHelper.IsLogEnabled != value)
                {
                    LogHelper.IsLogEnabled = value;
                    OnPropertyChanged(nameof(IsLogEnabled));
                    SetSettingsAndNotify(IsLogEnableSettingKey, value);
                }
            }
        }

        public HttpClientProxyModel? HttpClientProxy
        {
            get => httpClientProxy;
            set => ChangeSettings(ref httpClientProxy, value, HttpClientProxySettingKey);
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

                App.Current.SettingsView?.SetForegroundWindow();
            }
            catch (Exception ex)
            {
                HotLyric.Win32.Utils.LogHelper.LogError(ex);
            }
        }

        public AsyncRelayCommand ChangeProxyCmd => (changeProxyCmd ??= new AsyncRelayCommand(async () =>
        {
            var dialog = new SetProxyDialog(httpClientProxy);
            var result = await dialog.ShowModalWindowAsync(new ShowDialogOptions(default, DialogWindowStartupLocation.CenterScreen));
            if (result == ContentDialogResult.Primary)
            {
                HttpClientProxy = dialog.Proxy;
                HttpClientManager.Proxy = dialog.Proxy?.CreateConfigure();
            }
        }));

        public AsyncRelayCommand DeleteLogFilesWhenLogDisabledCmd => (deleteLogFilesWhenLogDisabledCmd ??= new AsyncRelayCommand(async () =>
        {
            await Task.Yield();

            if (!LogHelper.IsLogEnabled)
            {
                for (int i = 0; i < 3; i++)
                {
                    try
                    {
                        var folder = System.IO.Path.Combine(
                            ApplicationData.Current.LocalCacheFolder.Path,
                            "Local",
                            "Logs");

                        if (System.IO.Directory.Exists(folder))
                        {
                            System.IO.Directory.Delete(folder, true);
                        }
                    }
                    catch
                    {
                        await Task.Delay(100);
                    }
                }
            }
        }));

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
    }

    public enum LowFrameRateMode
    {
        Auto,
        Enabled,
        Disabled
    }
}
