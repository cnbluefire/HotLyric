using HotLyric.Win32.Utils.MediaSessions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows.Input;
using Windows.ApplicationModel;
using Windows.Media.Control;
using Microsoft.UI.Xaml.Media;
using HotLyric.Win32.Models.AppConfigurationModels;
using Microsoft.UI.Xaml.Media.Imaging;

namespace HotLyric.Win32.Utils.MediaSessions.SMTC
{
    public partial class SMTCSession : ISMTCSession
    {
        private bool disposedValue;
        private IReadOnlyList<GlobalSystemMediaTransportControlsSession> sessions;
        private string appUserModelId;
        private TaskCompletionSource<MediaSessionMediaProperties?>? mediaPropertiesSource;
        private GlobalSystemMediaTransportControlsSessionPlaybackInfo? playbackInfo;
        private GlobalSystemMediaTransportControlsSessionTimelineProperties? timelineProperties;
        private TaskCompletionSource<Package?>? packageSource;
        private AppConfigurationModel.MediaSessionAppModel app;

        private SMTCCommand playCommand;
        private SMTCCommand pauseCommand;
        private SMTCCommand skipPreviousCommand;
        private SMTCCommand skipNextCommand;

        private TimeSpan lastPosition = TimeSpan.Zero;
        private DateTime lastUpdatePositionTime = default;
        private Timer? internalPositionTimer;

        public SMTCSession(
            GlobalSystemMediaTransportControlsSession session,
            AppConfigurationModel.MediaSessionAppModel app) :
            this(new[] { session }, app)
        { }

        public SMTCSession(
            IReadOnlyList<GlobalSystemMediaTransportControlsSession> sessions,
            AppConfigurationModel.MediaSessionAppModel app)
        {
            if (sessions == null || sessions.Count == 0 || sessions.Any(c => c == null))
                throw new ArgumentNullException(nameof(sessions));

            this.sessions = sessions.ToArray();
            PositionMode = app.Options.PositionMode;
            this.app = app;
            appUserModelId = sessions[0].SourceAppUserModelId;

            foreach (var session in sessions)
            {
                session.MediaPropertiesChanged += Session_MediaPropertiesChanged;
                session.TimelinePropertiesChanged += Session_TimelinePropertiesChanged;

                if (playbackInfo == null)
                {
                    playbackInfo = session.GetPlaybackInfo();
                    session.PlaybackInfoChanged += Session_PlaybackInfoChanged;

                    playCommand = new SMTCCommand(async () => await session.TryPlayAsync());
                    pauseCommand = new SMTCCommand(async () => await session.TryPauseAsync());
                    skipPreviousCommand = new SMTCCommand(async () => await session.TrySkipPreviousAsync());
                    skipNextCommand = new SMTCCommand(async () => await session.TrySkipNextAsync());
                }

                if (timelineProperties == null || timelineProperties.LastUpdatedTime.Year > 2000)
                {
                    timelineProperties = session.GetTimelineProperties();
                }
            }

            playCommand ??= new SMTCCommand(() => Task.CompletedTask);
            pauseCommand ??= new SMTCCommand(() => Task.CompletedTask);
            skipPreviousCommand ??= new SMTCCommand(() => Task.CompletedTask);
            skipNextCommand ??= new SMTCCommand(() => Task.CompletedTask);

            UpdateTimelineProperties();
            UpdatePlaybackInfo();

            if (PositionMode == MediaSessionPositionMode.FromAppAndUseTimer || PositionMode == MediaSessionPositionMode.OnlyUseTimer)
            {
                internalPositionTimer = new Timer(300);

                internalPositionTimer.Elapsed += InternalPositionTimer_Elapsed;

                if (PositionMode == MediaSessionPositionMode.OnlyUseTimer)
                {
                    lastUpdatePositionTime = DateTime.Now;
                    UpdateInternalTimerState();
                }
            }
        }

        private void InternalPositionTimer_Elapsed(object? sender, ElapsedEventArgs? e)
        {
            var pos = TimeSpan.FromSeconds((DateTime.Now - lastUpdatePositionTime).TotalSeconds * PlaybackRate + lastPosition.TotalSeconds);
            if (PositionMode == MediaSessionPositionMode.OnlyUseTimer
                || pos >= StartTime && pos <= EndTime)
            {
                Position = pos;
            }

            UpdateInternalTimerState();

            PlaybackInfoChanged?.Invoke(this, EventArgs.Empty);
        }

        private void UpdateInternalTimerState()
        {
            if (timelineProperties == null || PlaybackStatus != MediaSessionPlaybackStatus.Playing)
            {
                internalPositionTimer?.Stop();
            }
            else
            {
                lastPosition = Position;
                lastUpdatePositionTime = DateTime.Now;
                if (internalPositionTimer?.Enabled == false)
                {
                    internalPositionTimer?.Start();
                }
            }
        }

        private void Session_TimelinePropertiesChanged(GlobalSystemMediaTransportControlsSession sender, TimelinePropertiesChangedEventArgs args)
        {
            timelineProperties = sender.GetTimelineProperties();
            UpdateInternalTimerState();

            if (PositionMode != MediaSessionPositionMode.OnlyUseTimer)
            {
                UpdateTimelineProperties();
                PlaybackInfoChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        private void Session_PlaybackInfoChanged(GlobalSystemMediaTransportControlsSession sender, PlaybackInfoChangedEventArgs args)
        {
            playbackInfo = sender.GetPlaybackInfo();
            UpdatePlaybackInfo();
            PlaybackInfoChanged?.Invoke(this, EventArgs.Empty);
        }

        private void Session_MediaPropertiesChanged(GlobalSystemMediaTransportControlsSession sender, MediaPropertiesChangedEventArgs args)
        {
            if (PositionMode == MediaSessionPositionMode.OnlyUseTimer)
            {
                lastPosition = TimeSpan.Zero;
                lastUpdatePositionTime = DateTime.Now;
                Position = TimeSpan.Zero;
                PlaybackInfoChanged?.Invoke(this, EventArgs.Empty);
            }

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

                if (PositionMode != MediaSessionPositionMode.OnlyUseTimer)
                {
                    lastUpdatePositionTime = DateTime.Now;
                    lastPosition = Position;
                }
            }
            else
            {
                StartTime = TimeSpan.Zero;
                EndTime = TimeSpan.Zero;
                Position = TimeSpan.Zero;

                if (PositionMode != MediaSessionPositionMode.OnlyUseTimer)
                {
                    lastUpdatePositionTime = default;
                    lastPosition = TimeSpan.Zero;
                    internalPositionTimer?.Stop();
                }
            }
        }

        private void UpdatePlaybackInfo()
        {
            if (playbackInfo != null)
            {
                var rate = playbackInfo.PlaybackRate ?? 1;
                PlaybackRate = rate > 0.001 ? rate : 1;

                PlaybackStatus = playbackInfo.PlaybackStatus switch
                {
                    GlobalSystemMediaTransportControlsSessionPlaybackStatus.Closed => MediaSessionPlaybackStatus.Closed,
                    GlobalSystemMediaTransportControlsSessionPlaybackStatus.Opened => MediaSessionPlaybackStatus.Opened,
                    GlobalSystemMediaTransportControlsSessionPlaybackStatus.Changing => MediaSessionPlaybackStatus.Changing,
                    GlobalSystemMediaTransportControlsSessionPlaybackStatus.Stopped => MediaSessionPlaybackStatus.Stopped,
                    GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing => MediaSessionPlaybackStatus.Playing,
                    GlobalSystemMediaTransportControlsSessionPlaybackStatus.Paused => MediaSessionPlaybackStatus.Paused,
                    _ => MediaSessionPlaybackStatus.Closed
                };
            }
            else
            {
                PlaybackRate = 1;
                PlaybackStatus = MediaSessionPlaybackStatus.Stopped;
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

        public async Task<MediaSessionMediaProperties?> GetMediaPropertiesAsync()
        {
            var taskSource = mediaPropertiesSource;
            if (taskSource == null)
            {
                taskSource = new TaskCompletionSource<MediaSessionMediaProperties?>();
                mediaPropertiesSource = taskSource;
                try
                {
                    GlobalSystemMediaTransportControlsSessionMediaProperties? mediaProperties = null;

                    foreach (var session in sessions)
                    {
                        mediaProperties = await session?.TryGetMediaPropertiesAsync();

                        if (disposedValue) break;

                        if (mediaProperties != null && !string.IsNullOrEmpty(mediaProperties.Title))
                        {
                            break;
                        }
                    }

                    var playbackType = mediaProperties?.PlaybackType;

                    taskSource.SetResult(mediaProperties?.PlaybackType switch
                    {
                        global::Windows.Media.MediaPlaybackType.Video => null,
                        global::Windows.Media.MediaPlaybackType.Image => null,
                        _ => CreateMediaProperties(mediaProperties)
                    });
                }
                catch (Exception ex)
                {
                    HotLyric.Win32.Utils.LogHelper.LogError(ex);
                    mediaPropertiesSource = null;
                    return null;
                }
            }
            return await taskSource.Task.ConfigureAwait(false);
        }

        public GlobalSystemMediaTransportControlsSession Session => sessions[0];

        public double PlaybackRate { get; private set; }

        public TimeSpan StartTime { get; private set; }

        public TimeSpan EndTime { get; private set; }

        public TimeSpan Position { get; private set; }

        public MediaSessionPlaybackStatus PlaybackStatus { get; private set; }

        public string AppUserModelId => appUserModelId;


        public ICommand PlayCommand => playCommand;
        public ICommand PauseCommand => pauseCommand;
        public ICommand SkipPreviousCommand => skipPreviousCommand;
        public ICommand SkipNextCommand => skipNextCommand;

        public MediaSessionPositionMode PositionMode { get; }

        public AppConfigurationModel.MediaSessionAppModel App => app;

        public bool IsDisposed => disposedValue;

        private MediaSessionMediaProperties? CreateMediaProperties(GlobalSystemMediaTransportControlsSessionMediaProperties? mediaProperties)
        {
            switch (app.Options.MediaPropertiesMode)
            {
                case MediaPropertiesMode.NCMClient:
                    return NCMClientCreateMediaProperties(mediaProperties);

                case MediaPropertiesMode.AppleMusic:
                    return AppleMusicCreateMediaProperties(mediaProperties);

                default:
                case MediaPropertiesMode.Default:
                    return DefaultCreateMediaProperties(mediaProperties);

            }
        }

        private async Task<Package?> GetAppPackageAsync()
        {
            if (packageSource == null)
            {
                packageSource = new TaskCompletionSource<Package?>();
                var package = await ApplicationHelper.TryGetPackageFromAppUserModelIdAsync(appUserModelId);
                packageSource.TrySetResult(package);
            }

            return await packageSource.Task.ConfigureAwait(false);
        }


        public async Task<string?> GetSessionNameAsync()
        {
            if (!string.IsNullOrEmpty(App.AppInfo?.DisplayName)) return App.AppInfo.DisplayName;

            var package = await GetAppPackageAsync();
            return package?.DisplayName;
        }

        public async Task<ImageSource?> GetSessionIconAsync()
        {
            var iconUri = App.AppInfo?.Icon;
            if (iconUri != null)
            {
                if (iconUri.Scheme.Equals("file", StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        if (File.Exists(iconUri.AbsolutePath))
                        {
                            var bitmap = new BitmapImage();
                            await bitmap.SetSourceAsync(File.OpenRead(iconUri.AbsolutePath).AsRandomAccessStream());
                            return bitmap;
                        }
                    }
                    catch { }
                }
                else
                {
                    return new BitmapImage(iconUri);
                }
            }

            var package = await GetAppPackageAsync();
            if (package != null)
            {
                return await ApplicationHelper.GetPackageIconAsync(package);
            }
            return null;
        }

        public event EventHandler? PlaybackInfoChanged;

        public event EventHandler? MediaPropertiesChanged;


        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: 释放托管状态(托管对象)

                    if (internalPositionTimer != null)
                    {
                        internalPositionTimer.Elapsed -= InternalPositionTimer_Elapsed;
                        internalPositionTimer.Stop();
                        internalPositionTimer = null;
                    }

                    playCommand.CanExecute = false;
                    pauseCommand.CanExecute = false;
                    skipPreviousCommand.CanExecute = false;
                    skipNextCommand.CanExecute = false;

                    foreach (var session in sessions)
                    {
                        session.MediaPropertiesChanged -= Session_MediaPropertiesChanged;
                        session.PlaybackInfoChanged -= Session_PlaybackInfoChanged;
                        session.TimelinePropertiesChanged -= Session_TimelinePropertiesChanged;
                    }

                    mediaPropertiesSource = null;
                    playbackInfo = null;
                    timelineProperties = null;

                    UpdateTimelineProperties();
                    UpdatePlaybackInfo();

                    sessions = null!;
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


        private static MediaSessionMediaProperties? DefaultCreateMediaProperties(GlobalSystemMediaTransportControlsSessionMediaProperties? mediaProperties)
        {
            if (mediaProperties == null) return null;

            var genres = mediaProperties.Genres?.ToArray();

            return new MediaSessionMediaProperties(
                mediaProperties.AlbumArtist,
                mediaProperties.AlbumTitle,
                mediaProperties.AlbumTrackCount,
                mediaProperties.Artist,
                "",
                "",
                genres ?? Array.Empty<string>(),
                mediaProperties.Subtitle,
                mediaProperties.Title,
                mediaProperties.TrackNumber);
        }

        private static MediaSessionMediaProperties? AppleMusicCreateMediaProperties(GlobalSystemMediaTransportControlsSessionMediaProperties? mediaProperties)
        {
            if (mediaProperties == null) return null;

            var albumArtist = mediaProperties.AlbumArtist;
            var arr = albumArtist.Split(" — ");

            var artist = mediaProperties.Artist;
            var album = mediaProperties.Title;

            if (arr.Length >= 2)
            {
                if (string.IsNullOrEmpty(artist)) artist = arr[0];
                if (string.IsNullOrEmpty(album)) album = arr[1];
            }
            else if (string.IsNullOrEmpty(artist))
            {
                artist = albumArtist;
            }

            var genres = mediaProperties.Genres?.ToArray();

            return new MediaSessionMediaProperties(
                albumArtist,
                album,
                mediaProperties.AlbumTrackCount,
                artist,
                "",
                "",
                genres ?? Array.Empty<string>(),
                mediaProperties.Subtitle,
                mediaProperties.Title,
                mediaProperties.TrackNumber);
        }
        private static MediaSessionMediaProperties? NCMClientCreateMediaProperties(GlobalSystemMediaTransportControlsSessionMediaProperties? mediaProperties)
        {
            if (mediaProperties == null) return null;

            int skip = 0;

            var neteaseMusicId = string.Empty;
            var localLrcPath = string.Empty;

            var genres = mediaProperties.Genres?.ToArray();

            if (genres != null)
            {
                if (genres.Length > 0 && genres[0]?.StartsWith("ncm-", StringComparison.OrdinalIgnoreCase) == true)
                {
                    neteaseMusicId = genres[0].Substring(4);
                    skip++;
                }

                if (genres.Length > 1
                    && !string.IsNullOrEmpty(genres[1])
                    && genres[1].Trim() is string path
                    && !System.IO.Path.IsPathRooted(path))
                {
                    localLrcPath = path;
                    skip++;
                }
            }

            if (skip > 0)
            {
                genres = genres?.Skip(skip).ToArray();
            }

            return new MediaSessionMediaProperties(
                mediaProperties.AlbumArtist,
                mediaProperties.AlbumTitle,
                mediaProperties.AlbumTrackCount,
                mediaProperties.Artist,
                neteaseMusicId,
                localLrcPath,
                genres ?? Array.Empty<string>(),
                mediaProperties.Subtitle,
                mediaProperties.Title,
                mediaProperties.TrackNumber);
        }
    }
}
