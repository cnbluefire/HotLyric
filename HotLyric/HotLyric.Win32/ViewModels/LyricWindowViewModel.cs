using HotLyric.Win32.Controls;
using HotLyric.Win32.Models;
using HotLyric.Win32.Utils;
using HotLyric.Win32.Utils.MediaSessions;
using HotLyric.Win32.Utils.MediaSessions.SMTC;
using HotLyric.Win32.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Windows.UI.Xaml;
using Microsoft.UI.Xaml;
using HotLyric.Win32.Controls.LyricControlDrawingData;

namespace HotLyric.Win32.ViewModels
{
    public partial class LyricWindowViewModel : ObservableObject
    {
        private readonly MediaSessionManagerFactory smtcFactory;
        private readonly SettingsWindowViewModel settingVm;
        private SMTCManager? smtcManager;
        private ISMTCSession[]? sessions;
        private PowerModeHelper powerModeHelper;

        public LyricWindowViewModel(MediaSessionManagerFactory smtcFactory, SettingsWindowViewModel settingVm)
        {
            this.smtcFactory = smtcFactory;
            this.settingVm = settingVm;

            isBackgroundTransientVisible = new DelayValueHolder<bool>(TimeSpan.FromSeconds(3));
            isBackgroundTransientVisible.ValueChanged += (s, a) =>
            {
                OnPropertyChanged(nameof(IsBackgroundTransientVisible));
                OnPropertyChanged(nameof(IsBackgroundVisible));
                OnPropertyChanged(nameof(IsTitleButtonVisible));
                OnPropertyChanged(nameof(LyricOpacity));
            };

            isMinimizedByPause = new DelayValueHolder<bool>(true, TimeSpan.FromSeconds(2));
            isMinimizedByPause.ValueChanged += (s, a) =>
            {
                OnPropertyChanged(nameof(ActualMinimized));
            };

            InitSessions();

            powerModeHelper = new PowerModeHelper();

            settingVm.SettingsChanged += SettingVm_SettingsChanged;
            UpdateSettings();

            powerModeHelper.PropertiesChanged += PowerModeHelper_PropertiesChanged;
        }

        private MediaSessionModel? selectedSession;

        private bool isTitleVisible;
        private bool alwaysShowBackground;
        private LyricDrawingLineAlignment lyricAlignment;
        private LyricControlLineMode lineMode;
        private bool isLyricTranslateEnabled;
        private bool isTransparent;
        private LyricControlTextStrokeType textStrokeType;
        private LyricControlScrollAnimationMode scrollAnimationMode = LyricControlScrollAnimationMode.Fast;


        private ObservableCollection<MediaSessionModel>? sessionModels;

        private MediaModel? mediaModel = MediaModel.CreateEmptyMedia();
        private bool isMinimized;
        private DelayValueHolder<bool> isMinimizedByPause;
        private bool karaokeEnabled;
        private ICommand? openCurrentSessionAppCmd;
        private LyricThemeView? lyricTheme;
        private DelayValueHolder<bool> isBackgroundTransientVisible;
        private bool isMouseOver;

        private bool isPlaying;

        private string lyricPlaceholderText = "";
        private string lyricNextLinePlaceholderText = "";

        private bool lowFrameRateMode;

        private AsyncRelayCommand? onlyUseTimerHelpCmd;

        public SettingsWindowViewModel SettingViewModel => settingVm;

        public bool IsTitleVisible
        {
            get => isTitleVisible;
            set => SetProperty(ref isTitleVisible, value);
        }

        public bool IsTitleButtonVisible => !IsTransparent;

        public bool IsMouseOver
        {
            get => isMouseOver;
            set
            {
                if (SetProperty(ref isMouseOver, value))
                {
                    if (!value)
                    {
                        IsBackgroundTransientVisible = false;
                    }
                    OnPropertyChanged(nameof(IsBackgroundVisible));
                    OnPropertyChanged(nameof(LyricOpacity));
                }
            }
        }

        public bool IsBackgroundTransientVisible
        {
            get => isBackgroundTransientVisible.Value;
            set => isBackgroundTransientVisible.Value = value;
        }

        public bool AlwaysShowBackground
        {
            get => alwaysShowBackground;
            private set
            {
                if (SetProperty(ref alwaysShowBackground, value))
                {
                    OnPropertyChanged(nameof(IsBackgroundVisible));
                    OnPropertyChanged(nameof(LyricOpacity));
                }

                isBackgroundTransientVisible.Value = false;
            }
        }

        public LyricDrawingLineAlignment LyricAlignment
        {
            get => lyricAlignment;
            private set => SetProperty(ref lyricAlignment, value);
        }

        public LyricControlLineMode LineMode
        {
            get => lineMode;
            private set => SetProperty(ref lineMode, value);
        }

        public bool IsLyricTranslateEnabled
        {
            get => isLyricTranslateEnabled;
            private set => SetProperty(ref isLyricTranslateEnabled, value);
        }

        public bool IsBackgroundVisible => !ActualMinimized && (IsMouseOver || IsBackgroundTransientVisible || AlwaysShowBackground);

        public bool IsMinimized
        {
            get => isMinimized;
            set
            {
                if (SetProperty(ref isMinimized, value))
                {
                    OnPropertyChanged(nameof(ActualMinimized));
                    OnPropertyChanged(nameof(IsBackgroundVisible));
                    OnPropertyChanged(nameof(LyricOpacity));

                    if (!isMinimized && !IsBackgroundVisible)
                    {
                        ShowBackgroundTransient(TimeSpan.FromSeconds(2));
                    }
                }
            }
        }

        public bool ActualMinimized => SelectedSession == null || IsMinimized || MediaModel == null || isMinimizedByPause.Value;

        public bool IsTransparent
        {
            get => isTransparent;
            private set
            {
                if (SetProperty(ref isTransparent, value))
                {
                    OnPropertyChanged(nameof(IsTitleButtonVisible));
                    if (value)
                    {
                        IsMouseOver = false;
                    }
                }
            }
        }

        public LyricControlTextStrokeType TextStrokeType
        {
            get => textStrokeType;
            private set => SetProperty(ref textStrokeType, value);
        }

        public LyricControlScrollAnimationMode ScrollAnimationMode
        {
            get => scrollAnimationMode;
            private set => SetProperty(ref scrollAnimationMode, value);
        }

        public LyricControlProgressAnimationMode ProgressAnimationMode =>
            (KaraokeEnabled && MediaModel?.HasLyric == true) ? LyricControlProgressAnimationMode.Karaoke
                : LyricControlProgressAnimationMode.Disabled;

        public bool KaraokeEnabled
        {
            get => karaokeEnabled;
            private set
            {
                if (SetProperty(ref karaokeEnabled, value))
                {
                    OnPropertyChanged(nameof(ProgressAnimationMode));
                }
            }
        }

        public double LyricOpacity => IsBackgroundVisible ? 1d : settingVm.LyricOpacity;

        public ObservableCollection<MediaSessionModel>? SessionModels
        {
            get => sessionModels;
            set => SetProperty(ref sessionModels, value);
        }

        public bool IsPlaying
        {
            get => isPlaying;
            private set
            {
                if (SetProperty(ref isPlaying, value))
                {
                    UpdateMinimizedByPause();
                }
            }
        }

        private WeakEventListener<MediaModel, object, PropertyChangedEventArgs>? mediaModelPropertyChangedWeakEvent;

        public MediaModel? MediaModel
        {
            get => mediaModel;
            set
            {
                var oldModel = mediaModel;
                if (SetProperty(ref mediaModel, value))
                {
                    OnPropertyChanged(nameof(ActualMinimized));
                    OnPropertyChanged(nameof(IsBackgroundVisible));
                    OnPropertyChanged(nameof(ProgressAnimationMode));
                    OnPropertyChanged(nameof(LyricOpacity));

                    UpdateLyricPlaceholder();

                    //App.Current.NotifyIcon?.UpdateToolTipText();

                    if (mediaModelPropertyChangedWeakEvent != null)
                    {
                        mediaModelPropertyChangedWeakEvent.Detach();
                        mediaModelPropertyChangedWeakEvent = null;
                    }

                    if (mediaModel != null)
                    {
                        mediaModelPropertyChangedWeakEvent = new WeakEventListener<MediaModel, object, PropertyChangedEventArgs>(mediaModel);
                        mediaModelPropertyChangedWeakEvent.OnEventAction = (i, s, a) => MediaModel_PropertyChanged(s, a);
                        mediaModelPropertyChangedWeakEvent.OnDetachAction =
                            e => mediaModel.PropertyChanged -= mediaModelPropertyChangedWeakEvent.OnEvent;
                        mediaModel.PropertyChanged += mediaModelPropertyChangedWeakEvent.OnEvent;
                    }
                }
            }
        }

        private void MediaModel_PropertyChanged(object? sender, PropertyChangedEventArgs? e)
        {
            UpdateLyricPlaceholder();
            OnPropertyChanged(nameof(ProgressAnimationMode));
            App.Current.NotifyIcon?.UpdateToolTipText();
        }

        public MediaSessionModel? SelectedSession
        {
            get => selectedSession;
            set
            {
                var old = selectedSession;
                var oldId = old?.AppTitle;
                if (SetProperty(ref selectedSession, value))
                {
                    if (old != null)
                    {
                        old.PlaybackInfoChanged -= SelectedSession_PlaybackInfoChanged;
                        old.MediaChanged -= SelectedSession_MediaChanged;
                    }

                    MediaModel? model;

                    if (selectedSession != null)
                    {
                        selectedSession.PlaybackInfoChanged += SelectedSession_PlaybackInfoChanged;
                        selectedSession.MediaChanged += SelectedSession_MediaChanged;
                        model = selectedSession?.CreateMediaModel();
                    }
                    else
                    {
                        model = MediaModel.CreateEmptyMedia();
                    }

                    if (MediaModel != model)
                    {
                        MediaModel?.Cancel();
                        MediaModel = model;
                        MediaModel?.StartLoad();
                    }

                }
                IsTitleVisible = SelectedSession != null;

                IsPlaying = SelectedSession?.IsPlaying ?? false;

                if (oldId != selectedSession?.AppTitle && !IsBackgroundVisible)
                {
                    ShowBackgroundTransient(TimeSpan.FromSeconds(2));
                }
                OnPropertyChanged(nameof(ActualMinimized));
                OnPropertyChanged(nameof(IsBackgroundVisible));
                OnPropertyChanged(nameof(LyricOpacity));
                OnlyUseTimerHelpCmd.NotifyCanExecuteChanged();
                OnPropertyChanged(nameof(OnlyUseTimerHelpButtonVisible));
                //App.Current.NotifyIcon?.UpdateToolTipText();
            }
        }


        public ICommand OpenCurrentSessionAppCmd => openCurrentSessionAppCmd ?? (openCurrentSessionAppCmd = new AsyncRelayCommand(async () =>
        {
            if (SelectedSession?.Session?.App is SMTCApp app && app.SupportLaunch)
            {
                var curSessionAUMID = (SelectedSession?.Session as ISMTCSession)?.Session?.SourceAppUserModelId;
                if (string.IsNullOrEmpty(curSessionAUMID)) return;

                var package = await ApplicationHelper.TryGetPackageFromAppUserModelIdAsync(curSessionAUMID!);
                var pfn = package?.Id?.FamilyName;

                if (!string.IsNullOrEmpty(pfn))
                {
                    await ApplicationHelper.TryLaunchAppAsync(pfn!);
                }
            }
        }));

        public bool HasMoreSession => sessions != null && sessions.Length > 1;

        public LyricThemeView? LyricTheme
        {
            get => lyricTheme;
            private set
            {
                if (SetProperty(ref lyricTheme, value))
                {
                    ShowBackgroundTransient(TimeSpan.FromSeconds(3));
                }
            }
        }


        #region SMTC Session

        private async void InitSessions()
        {
            smtcManager = await smtcFactory.GetManagerAsync();
            if (smtcManager != null)
            {
                sessions = smtcManager.Sessions.ToArray();

                ActivationArgumentsHelper.ActivateMainInstanceEventReceived += CommandLineArgsHelper_ActivateMainInstanceEventReceived;

                UpdateSessions();
                smtcManager.SessionsChanged += SmtcManager_SessionsChanged;
            }
        }

        private void CommandLineArgsHelper_ActivateMainInstanceEventReceived(object? sender, EventArgs e)
        {
            UpdateSessions();
        }

        private void SelectedSession_MediaChanged(object? sender, EventArgs e)
        {
            var model = SelectedSession?.CreateMediaModel();
            if (MediaModel != model)
            {
                MediaModel?.Cancel();
                MediaModel = model;
                MediaModel?.StartLoad();
            }
        }


        private void SelectedSession_PlaybackInfoChanged(object? sender, EventArgs e)
        {
            IsPlaying = SelectedSession?.IsPlaying ?? false;
        }

        private void UpdateMinimizedByPause()
        {
            if (settingVm.HideOnPaused)
            {
                var isPlaying = SelectedSession?.IsPlaying ?? false;

                if (isPlaying)
                {
                    isMinimizedByPause.Value = false;
                }
                else
                {
                    // 如果不存在延迟值，或延迟值不是true，则设置延迟值
                    if (!isMinimizedByPause.HasNextValue || !isMinimizedByPause.NextValue)
                    {
                        isMinimizedByPause.SetValueDelay(true);
                    }
                }
            }
            else
            {
                isMinimizedByPause.Value = false;
            }
        }

        private void SmtcManager_SessionsChanged(object? sender, EventArgs e)
        {
            App.DispatcherQueue.TryEnqueue(() =>
            {
                if (smtcManager != null)
                {
                    sessions = smtcManager.Sessions.ToArray();

                    UpdateSessions();
                }
            });
        }

        private ISMTCSession? GetNamedSession(string? appId)
        {
            if (sessions == null || sessions.Length == 0) return null;

            if (string.IsNullOrEmpty(appId)) return null;

            var prefix = appId.Substring(0, appId.IndexOf("_") + 1);
            if (string.IsNullOrEmpty(prefix)) return null;

            foreach (var session in sessions)
            {
                if (!session.IsDisposed && session.Session.SourceAppUserModelId?.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) == true)
                {
                    return session;
                }
            }

            return null;
        }

        private async void UpdateSessions()
        {
            string? from = "";
            if (ActivationArgumentsHelper.HasLaunchParameters)
            {
                from = ActivationArgumentsHelper.LaunchFromPackageFamilyName;

                // 从参数启动时不弹出启动app的窗口
            }

            var lastSelectedAppId = (SelectedSession?.Session as ISMTCSession)?.Session?.SourceAppUserModelId ?? "";
            if (smtcManager != null)
            {
                var curSession = GetNamedSession(from);

                if (curSession != null)
                {
                    // 启动参数已消费
                    ActivationArgumentsHelper.LaunchFromPackageFamilyName = null;
                }
                else
                {
                    curSession = GetNamedSession(lastSelectedAppId) ?? smtcManager.CurrentSession;
                }

                lastSelectedAppId = curSession?.Session?.SourceAppUserModelId ?? string.Empty;

                var _sessions = sessions ?? Array.Empty<ISMTCSession>();

                var models = await Task.WhenAll(_sessions.Select(async c => await MediaSessionModel.CreateAsync(c)));

                App.DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, () =>
                {
                    var oldSessionModels = SessionModels?.ToArray();

                    if (models != null)
                    {
                        SessionModels = new ObservableCollection<MediaSessionModel>(models!);
                    }
                    else
                    {
                        SessionModels = null;
                    }

                    SelectedSession = models?.FirstOrDefault(c => (c?.Session as ISMTCSession)?.Session?.SourceAppUserModelId == lastSelectedAppId)
                        ?? models?.FirstOrDefault();


                    if (oldSessionModels != null)
                    {
                        foreach (var model in oldSessionModels)
                        {
                            model.Dispose();
                        }
                    }

                    OnPropertyChanged(nameof(HasMoreSession));
                });
            }
        }

        #endregion SMTC Session

        private void SettingVm_SettingsChanged(object? sender, EventArgs e)
        {
            UpdateSettings();
        }

        public string LyricPlaceholderText
        {
            get => lyricPlaceholderText;
            private set => SetProperty(ref lyricPlaceholderText, value);
        }

        public string LyricNextLinePlaceholderText
        {
            get => lyricNextLinePlaceholderText;
            private set => SetProperty(ref lyricNextLinePlaceholderText, value);
        }

        private void UpdateLyricPlaceholder()
        {
            LyricPlaceholderText = MediaModel?.Name ?? "";
            LyricNextLinePlaceholderText = MediaModel?.Artist ?? "";
        }

        public bool LowFrameRateMode
        {
            get => lowFrameRateMode;
            private set => SetProperty(ref lowFrameRateMode, value);
        }

        public AsyncRelayCommand OnlyUseTimerHelpCmd => onlyUseTimerHelpCmd ?? (onlyUseTimerHelpCmd = new AsyncRelayCommand(async () =>
        {
            var uri = new Uri("https://github.com/cnbluefire/HotLyric#%E5%AF%B9%E9%83%A8%E5%88%86%E8%BD%AF%E4%BB%B6%E6%8F%90%E4%BE%9B%E6%9C%89%E9%99%90%E6%94%AF%E6%8C%81");
            await Windows.System.Launcher.LaunchUriAsync(uri);
        }, () => SelectedSession?.Session is SMTCSession session && session.PositionMode == SMTCAppPositionMode.OnlyUseTimer && !OnlyUseTimerHelpCmd.IsRunning));

        public bool OnlyUseTimerHelpButtonVisible => OnlyUseTimerHelpCmd.CanExecute(null);

        private void PowerModeHelper_PropertiesChanged(object? sender, EventArgs e)
        {
            App.DispatcherQueue.TryEnqueue(UpdatePowerMode);
        }

        public void UpdatePowerMode()
        {
            NLog.LogManager.GetCurrentClassLogger().Info($"{powerModeHelper.EffectivePowerMode} {powerModeHelper.BatteryStatus}");

            var lowFrameRateMode = settingVm.LowFrameRateMode.SelectedValue ?? ViewModels.LowFrameRateMode.Auto;
            bool enableEfficiencyMode = false;
            bool enableLowFrameRateMode = false;

            if (lowFrameRateMode == ViewModels.LowFrameRateMode.Enabled)
            {
                enableLowFrameRateMode = true;
                enableEfficiencyMode = true;
            }
            else if (lowFrameRateMode == ViewModels.LowFrameRateMode.Auto)
            {
                var powerMode = powerModeHelper.EffectivePowerMode;
                var batteryStatus = powerModeHelper.BatteryStatus;

                switch (powerMode)
                {
                    case Microsoft.Windows.System.Power.EffectivePowerMode.BatterySaver:
                    case Microsoft.Windows.System.Power.EffectivePowerMode.BetterBattery:
                        enableLowFrameRateMode = true;
                        enableEfficiencyMode = true;
                        break;

                    case Microsoft.Windows.System.Power.EffectivePowerMode.Balanced:
                        if (batteryStatus == Microsoft.Windows.System.Power.BatteryStatus.Discharging)
                        {
                            enableEfficiencyMode = true;
                        }
                        break;

                    case Microsoft.Windows.System.Power.EffectivePowerMode.HighPerformance:
                    case Microsoft.Windows.System.Power.EffectivePowerMode.MaxPerformance:
                    case Microsoft.Windows.System.Power.EffectivePowerMode.GameMode:
                    case Microsoft.Windows.System.Power.EffectivePowerMode.MixedReality:
                    default:

                        break;
                }
            }

            PowerModeHelper.SetEfficiencyMode(enableEfficiencyMode);
            LowFrameRateMode = enableLowFrameRateMode;

            if (lowFrameRateMode == ViewModels.LowFrameRateMode.Disabled)
            {
                ForegroundWindowHelper.Uninitialize();

            }
            else
            {
                ForegroundWindowHelper.Initialize();
            }

            if (App.Current.LyricView?.TopmostHelper != null)
            {
                App.Current.LyricView.TopmostHelper.PowerMode = powerModeHelper.EffectivePowerMode;
            }
        }

        private void UpdateSettings()
        {
            IsTransparent = settingVm.WindowTransparent;

            switch (settingVm.SecondRowTypes.SelectedValue)
            {
                case SecondRowType.Collapsed:
                    LineMode = LyricControlLineMode.SingleLine;
                    break;

                case SecondRowType.TranslationOrNextLyric:
                    LineMode = LyricControlLineMode.DoubleLine;
                    IsLyricTranslateEnabled = true;
                    break;

                case SecondRowType.NextLyricOnly:
                    LineMode = LyricControlLineMode.DoubleLine;
                    IsLyricTranslateEnabled = false;
                    break;
            }

            KaraokeEnabled = settingVm.KaraokeEnabled;
            LyricAlignment = settingVm.LyricAlignments.SelectedValue ?? LyricDrawingLineAlignment.Left;

            AlwaysShowBackground = settingVm.AlwaysShowBackground;
            TextStrokeType = settingVm.TextStrokeTypes.SelectedValue ?? LyricControlTextStrokeType.Auto;

            LyricTheme = settingVm.CurrentTheme;

            ScrollAnimationMode = settingVm.ScrollAnimationMode.SelectedValue ?? LyricControlScrollAnimationMode.Fast;

            UpdateMinimizedByPause();

            OnPropertyChanged(nameof(LyricOpacity));

            UpdatePowerMode();

            if (App.Current.LyricView?.TopmostHelper != null)
            {
                App.Current.LyricView.TopmostHelper.HideWhenFullScreenAppOpen = settingVm.HideWhenFullScreenAppOpen;
                App.Current.LyricView.TopmostHelper.Enabled = settingVm.LowFrameRateMode.SelectedValue != ViewModels.LowFrameRateMode.Disabled;
            }
        }

        public void ShowBackgroundTransient(TimeSpan time)
        {
            if (ActualMinimized || AlwaysShowBackground) return;
            IsBackgroundTransientVisible = true;
            var cts = new CancellationTokenSource();

            isBackgroundTransientVisible.Value = true;
            isBackgroundTransientVisible.SetValueDelay(false, time);
        }
    }
}
