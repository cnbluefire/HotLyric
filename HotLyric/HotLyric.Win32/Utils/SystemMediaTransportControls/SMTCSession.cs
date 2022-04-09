using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Windows.ApplicationModel;
using Windows.Media.Control;

namespace HotLyric.Win32.Utils.SystemMediaTransportControls
{
    public class SMTCSession : IDisposable
    {
        private bool disposedValue;
        private GlobalSystemMediaTransportControlsSession session;
        private string appUserModelId;
        private TaskCompletionSource<GlobalSystemMediaTransportControlsSessionMediaProperties?>? mediaPropertiesSource;
        private GlobalSystemMediaTransportControlsSessionPlaybackInfo? playbackInfo;
        private GlobalSystemMediaTransportControlsSessionTimelineProperties? timelineProperties;
        private TaskCompletionSource<Package?>? packageSource;

        private InternalCommand playCommand;
        private InternalCommand pauseCommand;
        private InternalCommand skipPreviousCommand;
        private InternalCommand skipNextCommand;

        private TimeSpan lastPosition = TimeSpan.Zero;
        private DateTime lastUpdatePositionTime = default;
        private DispatcherTimer? internalPositionTimer;

        public SMTCSession(GlobalSystemMediaTransportControlsSession session, bool useTimer, string? customName, ImageSource? customAppIcon, bool supportLaunch)
        {
            this.session = session ?? throw new ArgumentNullException(nameof(session));
            CustomName = customName;
            CustomAppIcon = customAppIcon;
            SupportLaunch = supportLaunch;
            appUserModelId = session.SourceAppUserModelId;

            session.MediaPropertiesChanged += Session_MediaPropertiesChanged;
            session.PlaybackInfoChanged += Session_PlaybackInfoChanged;
            session.TimelinePropertiesChanged += Session_TimelinePropertiesChanged;

            playbackInfo = session.GetPlaybackInfo();
            timelineProperties = session.GetTimelineProperties();

            playCommand = new InternalCommand(async () => await session.TryPlayAsync());
            pauseCommand = new InternalCommand(async () => await session.TryPauseAsync());
            skipPreviousCommand = new InternalCommand(async () => await session.TrySkipPreviousAsync());
            skipNextCommand = new InternalCommand(async () => await session.TrySkipNextAsync());

            UpdateTimelineProperties();
            UpdatePlaybackInfo();

            if (useTimer)
            {
                internalPositionTimer = new DispatcherTimer()
                {
                    Interval = TimeSpan.FromMilliseconds(300)
                };
                internalPositionTimer.Tick += InternalPositionTimer_Tick;
            }
        }

        private void InternalPositionTimer_Tick(object? sender, EventArgs e)
        {
            UpdateInternalTimerState();

            var pos = (DateTime.Now - lastUpdatePositionTime) * PlaybackRate + lastPosition;
            if (pos >= StartTime && pos <= EndTime)
            {
                Position = pos;
            }
            PlaybackInfoChanged?.Invoke(this, EventArgs.Empty);
        }

        private void UpdateInternalTimerState()
        {
            if (timelineProperties == null || PlaybackStatus != GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing)
            {
                internalPositionTimer?.Stop();
            }
            else
            {
                internalPositionTimer?.Start();
            }
        }

        private void Session_TimelinePropertiesChanged(GlobalSystemMediaTransportControlsSession sender, TimelinePropertiesChangedEventArgs args)
        {
            timelineProperties = session.GetTimelineProperties();
            UpdateInternalTimerState();
            UpdateTimelineProperties();
            PlaybackInfoChanged?.Invoke(this, EventArgs.Empty);
        }

        private void Session_PlaybackInfoChanged(GlobalSystemMediaTransportControlsSession sender, PlaybackInfoChangedEventArgs args)
        {
            playbackInfo = session.GetPlaybackInfo();
            UpdatePlaybackInfo();
            PlaybackInfoChanged?.Invoke(this, EventArgs.Empty);
        }

        private void Session_MediaPropertiesChanged(GlobalSystemMediaTransportControlsSession sender, MediaPropertiesChangedEventArgs args)
        {
            mediaPropertiesSource = null;
            MediaPropertiesChanged?.Invoke(this, EventArgs.Empty);
        }

        private void UpdateTimelineProperties()
        {
            if (timelineProperties != null)
            {
                StartTime = timelineProperties.StartTime;
                EndTime = timelineProperties.EndTime;
                Position = timelineProperties.Position;

                lastUpdatePositionTime = DateTime.Now;
                lastPosition = Position;
            }
            else
            {
                StartTime = TimeSpan.Zero;
                EndTime = TimeSpan.Zero;
                Position = TimeSpan.Zero;

                lastUpdatePositionTime = default;
                lastPosition = TimeSpan.Zero;
                internalPositionTimer?.Stop();
            }
        }

        private void UpdatePlaybackInfo()
        {
            if (playbackInfo != null)
            {
                var rate = playbackInfo.PlaybackRate ?? 1;
                PlaybackRate = rate > 0.001 ? rate : 1;

                PlaybackStatus = playbackInfo.PlaybackStatus;
            }
            else
            {
                PlaybackRate = 1;
                PlaybackStatus = GlobalSystemMediaTransportControlsSessionPlaybackStatus.Stopped;
            }

            UpdateInternalTimerState();

            UpdateControls();
        }

        private void UpdateControls()
        {
            if (playbackInfo != null)
            {
                playCommand.CanExecute = playbackInfo.Controls.IsPlayEnabled;
                pauseCommand.CanExecute = playbackInfo.Controls.IsPauseEnabled;
                skipPreviousCommand.CanExecute = playbackInfo.Controls.IsPreviousEnabled;
                skipNextCommand.CanExecute = playbackInfo.Controls.IsNextEnabled;
            }
            else
            {
                playCommand.CanExecute = false;
                pauseCommand.CanExecute = false;
                skipPreviousCommand.CanExecute = false;
                skipNextCommand.CanExecute = false;
            }
        }

        public double PlaybackRate { get; private set; }

        public TimeSpan StartTime { get; private set; }

        public TimeSpan EndTime { get; private set; }

        public TimeSpan Position { get; private set; }

        public GlobalSystemMediaTransportControlsSessionPlaybackStatus PlaybackStatus { get; private set; }

        public string AppUserModelId => appUserModelId;


        public ICommand PlayCommand => playCommand;
        public ICommand PauseCommand => pauseCommand;
        public ICommand SkipPreviousCommand => skipPreviousCommand;
        public ICommand SkipNextCommand => skipNextCommand;

        public string? CustomName { get; }

        public ImageSource? CustomAppIcon { get; }

        public bool SupportLaunch { get; }

        public async Task<GlobalSystemMediaTransportControlsSessionMediaProperties?> GetMediaPropertiesAsync()
        {
            var taskSource = mediaPropertiesSource;
            if (taskSource == null)
            {
                taskSource = new TaskCompletionSource<GlobalSystemMediaTransportControlsSessionMediaProperties?>();
                mediaPropertiesSource = taskSource;
                try
                {
                    var mediaProperties = await session.TryGetMediaPropertiesAsync();
                    taskSource.SetResult(mediaProperties);
                }
                catch
                {
                    mediaPropertiesSource = null;
                    return null;
                }
            }
            return await taskSource.Task.ConfigureAwait(false);
        }

        public async Task<Package?> GetAppPackageAsync()
        {
            if (packageSource == null)
            {
                packageSource = new TaskCompletionSource<Package?>();
                var package = await ApplicationHelper.TryGetPackageFromAppUserModelIdAsync(appUserModelId);
                packageSource.TrySetResult(package);
            }

            return await packageSource.Task.ConfigureAwait(false);
        }

        public event EventHandler? PlaybackInfoChanged;

        public event EventHandler? MediaPropertiesChanged;

        private class InternalCommand : ICommand, INotifyPropertyChanged
        {
            private static readonly PropertyChangedEventArgs CanExecutePropertyChangedEventArgs = new PropertyChangedEventArgs(nameof(CanExecute));

            private bool canExecute;
            private Func<Task> action;
            private Task? curTask;

            public InternalCommand(Func<Task> action)
            {
                this.action = action ?? throw new ArgumentNullException(nameof(action));
            }

            internal bool CanExecute
            {
                get => canExecute;
                set
                {
                    if (canExecute != value)
                    {
                        canExecute = value;
                        NotifyCanExecuteChanged();
                    }
                }
            }

            public event EventHandler? CanExecuteChanged;
            public event PropertyChangedEventHandler? PropertyChanged;

            bool ICommand.CanExecute(object? parameter)
            {
                return CanExecute;
            }

            public void Execute(object? parameter)
            {
                if (curTask != null) return;
                RunCore();

                async void RunCore()
                {
                    try
                    {
                        curTask = action.Invoke();
                        await curTask;
                    }
                    catch { }
                    finally
                    {
                        curTask = null;
                    }
                    NotifyCanExecuteChanged();
                }
            }

            internal void NotifyCanExecuteChanged()
            {
                _ = DispatcherHelper.UIDispatcher?.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(() =>
                {
                    CanExecuteChanged?.Invoke(this, EventArgs.Empty);
                    PropertyChanged?.Invoke(this, CanExecutePropertyChangedEventArgs);
                }));
            }
        }


        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: 释放托管状态(托管对象)

                    if (internalPositionTimer != null)
                    {
                        internalPositionTimer.Tick -= InternalPositionTimer_Tick;
                        internalPositionTimer.Stop();
                        internalPositionTimer = null;
                    }

                    playCommand.CanExecute = false;
                    pauseCommand.CanExecute = false;
                    skipPreviousCommand.CanExecute = false;
                    skipNextCommand.CanExecute = false;

                    session.MediaPropertiesChanged -= Session_MediaPropertiesChanged;
                    session.PlaybackInfoChanged -= Session_PlaybackInfoChanged;
                    session.TimelinePropertiesChanged -= Session_TimelinePropertiesChanged;

                    mediaPropertiesSource = null;
                    playbackInfo = null;
                    timelineProperties = null;

                    UpdateTimelineProperties();
                    UpdatePlaybackInfo();

                    session = null!;
                }

                // TODO: 释放未托管的资源(未托管的对象)并重写终结器
                // TODO: 将大型字段设置为 null
                disposedValue = true;
            }
        }

        // // TODO: 仅当“Dispose(bool disposing)”拥有用于释放未托管资源的代码时才替代终结器
        // ~SMTCSession()
        // {
        //     // 不要更改此代码。请将清理代码放入“Dispose(bool disposing)”方法中
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            // 不要更改此代码。请将清理代码放入“Dispose(bool disposing)”方法中
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
