using HotLyric.Win32.Controls;
using HotLyric.Win32.Models;
using HotLyric.Win32.Utils;
using HotLyric.Win32.Utils.MediaSessions;
using HotLyric.Win32.Utils.MediaSessions.SMTC;
using HotLyric.Win32.Utils.WindowBackgrounds;
using HotLyric.Win32.Views;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
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
using System.Windows.Media;
using Windows.System;

namespace HotLyric.Win32.ViewModels
{
    public partial class LyricWindowViewModel : ObservableObject
    {
        private readonly MediaSessionManagerFactory smtcFactory;
        private readonly SettingsWindowViewModel settingVm;
        private SMTCManager? smtcManager;
        private ISMTCSession[]? sessions;

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

            settingVm.SettingsChanged += SettingVm_SettingsChanged;
            UpdateSettings();
        }


        private MediaSessionModel? selectedSession;

        private bool isTitleVisible;
        private bool alwaysShowBackground;
        private HorizontalAlignment lyricHorizontalAlignment;
        private bool isTransparent;
        private bool showShadow;
        private bool textStrokeEnabled;

        private ObservableCollection<MediaSessionModel>? sessionModels;

        private MediaModel? mediaModel = MediaModel.CreateEmptyMedia();
        private bool isMinimized;
        private DelayValueHolder<bool> isMinimizedByPause;
        private bool karaokeEnabled;
        private ICommand? openCurrentSessionAppCmd;
        private LyricThemeView? lyricTheme;
        private DelayValueHolder<bool> isBackgroundTransientVisible;
        private WindowBackgroundHelper? backgroundHelper;

        private bool isPlaying;

        private string lyricPlaceholderText = "";
        private string lyricNextLinePlaceholderText = "";

        private AsyncRelayCommand? onlyUseTimerHelpCmd;

        public SettingsWindowViewModel SettingViewModel => settingVm;

        public WindowBackgroundHelper? BackgroundHelper
        {
            get => backgroundHelper;
            set
            {
                var old = backgroundHelper;
                if (SetProperty(ref backgroundHelper, value))
                {
                    if (old != null)
                    {
                        WeakEventManager<INotifyPropertyChanged, PropertyChangedEventArgs>.RemoveHandler(
                            old, "PropertyChanged", OnBackgroundHelperPropertyChanged);
                    }
                    if (backgroundHelper != null)
                    {
                        WeakEventManager<INotifyPropertyChanged, PropertyChangedEventArgs>.AddHandler(
                            backgroundHelper, "PropertyChanged", OnBackgroundHelperPropertyChanged);
                    }
                }
            }
        }

        private void OnBackgroundHelperPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (sender is WindowBackgroundHelper helper
                && e.PropertyName == nameof(helper.IsHitTestVisible))
            {
                IsBackgroundTransientVisible = helper.IsHitTestVisible;
            }
        }

        public bool IsTitleVisible
        {
            get => isTitleVisible;
            set => SetProperty(ref isTitleVisible, value);
        }

        public bool IsTitleButtonVisible => !IsTransparent;

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
                    if (backgroundHelper != null)
                    {
                        backgroundHelper.ForceVisible = value;
                    }

                    OnPropertyChanged(nameof(IsBackgroundVisible));
                    OnPropertyChanged(nameof(IsTitleButtonVisible));
                    OnPropertyChanged(nameof(LyricOpacity));
                }

                isBackgroundTransientVisible.Value = false;
            }
        }

        public HorizontalAlignment LyricHorizontalAlignment
        {
            get => lyricHorizontalAlignment;
            private set => SetProperty(ref lyricHorizontalAlignment, value);
        }

        public bool IsBackgroundVisible => !ActualMinimized && (IsBackgroundTransientVisible || AlwaysShowBackground);

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
                }
            }
        }

        public bool ShowShadow
        {
            get => showShadow;
            private set
            {
                if (SetProperty(ref showShadow, value))
                {
                    ShowBackgroundTransient(TimeSpan.FromSeconds(2));
                }
            }
        }

        public bool TextStrokeEnabled
        {
            get => textStrokeEnabled;
            private set => SetProperty(ref textStrokeEnabled, value);
        }

        public bool ActualKaraokeEnabled => KaraokeEnabled && MediaModel?.HasLyric == true;

        public bool KaraokeEnabled
        {
            get => karaokeEnabled;
            private set
            {
                if (SetProperty(ref karaokeEnabled, value))
                {
                    OnPropertyChanged(nameof(ActualKaraokeEnabled));
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
                    OnPropertyChanged(nameof(ActualKaraokeEnabled));
                    OnPropertyChanged(nameof(LyricOpacity));

                    UpdateLyricPlaceholder();

                    App.Current.NotifyIcon?.UpdateToolTipText();

                    if (oldModel != null)
                    {
                        WeakEventManager<MediaModel, PropertyChangedEventArgs>.RemoveHandler(oldModel, "PropertyChanged", new EventHandler<PropertyChangedEventArgs>(MediaModel_PropertyChanged));
                    }

                    if (mediaModel != null)
                    {
                        WeakEventManager<MediaModel, PropertyChangedEventArgs>.AddHandler(mediaModel, "PropertyChanged", new EventHandler<PropertyChangedEventArgs>(MediaModel_PropertyChanged));
                    }
                }
            }
        }

        private void MediaModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            UpdateLyricPlaceholder();
            OnPropertyChanged(nameof(ActualKaraokeEnabled));
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
                App.Current.NotifyIcon?.UpdateToolTipText();
            }
        }


        public ICommand OpenCurrentSessionAppCmd => openCurrentSessionAppCmd ?? (openCurrentSessionAppCmd = new AsyncRelayCommand(async () =>
        {
            if (SelectedSession?.Session?.App is SMTCApp app && app.SupportLaunch)
            {
                var curSessionAUMID = (SelectedSession?.Session as ISMTCSession)?.Session?.SourceAppUserModelId;
                if (string.IsNullOrEmpty(curSessionAUMID)) return;

                var package = await ApplicationHelper.TryGetPackageFromAppUserModelIdAsync(curSessionAUMID);
                var pfn = package?.Id?.FamilyName;

                if (!string.IsNullOrEmpty(pfn))
                {
                    await ApplicationHelper.TryLaunchAppAsync(pfn);
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

                CommandLineArgsHelper.ActivateMainInstanceEventReceived += CommandLineArgsHelper_ActivateMainInstanceEventReceived;

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
            DispatcherHelper.UIDispatcher!.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(() =>
            {
                if (smtcManager != null)
                {
                    sessions = smtcManager.Sessions.ToArray();

                    UpdateSessions();
                }
            }));
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
            if (CommandLineArgsHelper.HasLaunchParameters)
            {
                from = CommandLineArgsHelper.LaunchFromPackageFamilyName;

                // 从参数启动时不弹出启动app的窗口
            }

            var lastSelectedAppId = (SelectedSession?.Session as ISMTCSession)?.Session?.SourceAppUserModelId ?? "";
            if (smtcManager != null)
            {
                var curSession = GetNamedSession(from);

                if (curSession != null)
                {
                    // 启动参数已消费
                    CommandLineArgsHelper.LaunchFromPackageFamilyName = null;
                }
                else
                {
                    curSession = GetNamedSession(lastSelectedAppId) ?? smtcManager.CurrentSession;
                }

                lastSelectedAppId = curSession?.Session?.SourceAppUserModelId ?? string.Empty;

                var models = await Task.WhenAll(sessions.Select(async c => await MediaSessionModel.CreateAsync(c)));

                DispatcherHelper.UIDispatcher?.BeginInvoke(System.Windows.Threading.DispatcherPriority.Background, new Action(() =>
                {
                    SelectedSession = models?.FirstOrDefault(c => (c?.Session as ISMTCSession)?.Session?.SourceAppUserModelId == lastSelectedAppId)
                        ?? models?.FirstOrDefault();

                    var oldSessionModels = SessionModels?.ToArray();

                    if (models != null)
                    {
                        SessionModels = new ObservableCollection<MediaSessionModel>(models!);
                    }
                    else
                    {
                        SessionModels = null;
                    }

                    if (oldSessionModels != null)
                    {
                        foreach (var model in oldSessionModels)
                        {
                            model.Dispose();
                        }
                    }

                    OnPropertyChanged(nameof(HasMoreSession));
                }));
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

        public AsyncRelayCommand OnlyUseTimerHelpCmd => onlyUseTimerHelpCmd ?? (onlyUseTimerHelpCmd = new AsyncRelayCommand(async () =>
        {
            var uri = new Uri("https://github.com/cnbluefire/HotLyric#%E5%AF%B9%E9%83%A8%E5%88%86%E8%BD%AF%E4%BB%B6%E6%8F%90%E4%BE%9B%E6%9C%89%E9%99%90%E6%94%AF%E6%8C%81");
            await Launcher.LaunchUriAsync(uri);
        }, () => SelectedSession?.Session is SMTCSession session && session.PositionMode == SMTCAppPositionMode.OnlyUseTimer && !OnlyUseTimerHelpCmd.IsRunning));

        public bool OnlyUseTimerHelpButtonVisible => OnlyUseTimerHelpCmd.CanExecute(null);

        private void UpdateSettings()
        {
            IsTransparent = settingVm.WindowTransparent;
            //SecondRowType = settingVm.SecondRowType;
            KaraokeEnabled = settingVm.KaraokeEnabled;
            LyricHorizontalAlignment = settingVm.LyricHorizontalAlignment;

            AlwaysShowBackground = settingVm.AlwaysShowBackground;
            ShowShadow = settingVm.ShowShadow;
            TextStrokeEnabled = settingVm.TextStrokeEnabled;

            LyricTheme = settingVm.CurrentTheme;

            UpdateMinimizedByPause();

            OnPropertyChanged(nameof(LyricOpacity));
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
