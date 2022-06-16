using HotLyric.Win32.Utils.MediaSessions;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows.Input;
using System.Windows.Media;
using Windows.ApplicationModel;
using Windows.Media.Control;

namespace HotLyric.Win32.Utils.MediaSessions.SMTC
{
    public class YesPlayerMusicSession : ISMTCSession
    {
        private static HttpClient? httpClient;

        private bool disposedValue;
        private GlobalSystemMediaTransportControlsSession session;
        private string appUserModelId;
        private MediaSessionMediaProperties? mediaProperties;
        private GlobalSystemMediaTransportControlsSessionPlaybackInfo? playbackInfo;

        private SMTCCommand playCommand;
        private SMTCCommand pauseCommand;
        private SMTCCommand skipPreviousCommand;
        private SMTCCommand skipNextCommand;

        private Timer? internalPositionTimer;

        private TimeSpan endTime;
        private TimeSpan position;
        private Task? firstUpdateMediaPropertiesTask;

        public YesPlayerMusicSession(GlobalSystemMediaTransportControlsSession session, SMTCApp app)
        {
            this.session = session ?? throw new ArgumentNullException(nameof(session));

            if (httpClient == null)
            {
                httpClient = new HttpClient();
            }

            App = app;
            appUserModelId = session.SourceAppUserModelId;

            session.MediaPropertiesChanged += Session_MediaPropertiesChanged;
            session.PlaybackInfoChanged += Session_PlaybackInfoChanged;

            playbackInfo = session.GetPlaybackInfo();

            playCommand = new SMTCCommand(async () => await session.TryPlayAsync());
            pauseCommand = new SMTCCommand(async () => await session.TryPauseAsync());
            skipPreviousCommand = new SMTCCommand(async () => await session.TrySkipPreviousAsync());
            skipNextCommand = new SMTCCommand(async () => await session.TrySkipNextAsync());

            firstUpdateMediaPropertiesTask = UpdatePlayerInfoAsync();
            UpdatePlaybackInfo();

            internalPositionTimer = new Timer(300);
            internalPositionTimer.Elapsed += InternalPositionTimer_Elapsed;
            internalPositionTimer.Start();
            UpdateInternalTimerState();
        }

        private async Task UpdatePlayerInfoAsync()
        {
            internalPositionTimer?.Stop();
            const string requestUrl = "http://127.0.0.1:27232/player";
            try
            {
                var json = await httpClient!.GetStringAsync(requestUrl);
                if (!string.IsNullOrEmpty(json))
                {
                    var model = JsonConvert.DeserializeObject<PlayerInfo>(json);

                    bool mediaChanged = false;

                    if (model?.currentTrack != null && !string.IsNullOrEmpty(model.currentTrack.name))
                    {
                        var artists = "";
                        if (model.currentTrack.ar != null && model.currentTrack.ar.Length > 0)
                        {
                            artists = string.Join(", ", model.currentTrack.ar.Where(c => !string.IsNullOrEmpty(c.name)).Select(c => c.name));
                        }

                        var properties = new MediaSessionMediaProperties(
                            artists,
                            model.currentTrack.al?.name ?? "",
                            1,
                            artists,
                            $"ncm-{model.currentTrack.id}",
                            "",
                            Array.Empty<string>(),
                            "",
                            model.currentTrack.name,
                            1);

                        mediaChanged = mediaProperties?.NeteaseMusicId != properties?.NeteaseMusicId;
                        mediaProperties = properties;

                        var curEndTime = endTime;

                        endTime = TimeSpan.FromMilliseconds(Math.Max(0, model.currentTrack.dt));
                        position = TimeSpan.FromSeconds(Math.Max(0, model.progress));
                    }
                    else
                    {
                        mediaChanged = mediaProperties != null;

                        mediaProperties = null;
                        endTime = TimeSpan.Zero;
                        position = TimeSpan.Zero;
                    }

                    if (mediaChanged)
                    {
                        MediaPropertiesChanged?.Invoke(this, EventArgs.Empty);
                    }

                    PlaybackInfoChanged?.Invoke(this, EventArgs.Empty);
                }
            }
            catch { }

            firstUpdateMediaPropertiesTask = null;

            if (!disposedValue)
            {
                internalPositionTimer?.Start();
            }
        }

        private async void InternalPositionTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            await UpdatePlayerInfoAsync();
        }

        private void UpdateInternalTimerState()
        {
            if (PlaybackStatus != MediaSessionPlaybackStatus.Playing)
            {
                if (internalPositionTimer != null)
                {
                    internalPositionTimer.Interval = 1500;
                }
            }
            else
            {
                if (internalPositionTimer != null)
                {
                    internalPositionTimer.Interval = 300;
                }
            }
        }

        private void Session_PlaybackInfoChanged(GlobalSystemMediaTransportControlsSession sender, PlaybackInfoChangedEventArgs args)
        {
            playbackInfo = session.GetPlaybackInfo();
            UpdatePlaybackInfo();
            PlaybackInfoChanged?.Invoke(this, EventArgs.Empty);
        }

        private async void Session_MediaPropertiesChanged(GlobalSystemMediaTransportControlsSession sender, MediaPropertiesChangedEventArgs args)
        {
            await UpdatePlayerInfoAsync();
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
            var task = firstUpdateMediaPropertiesTask;
            if (task != null)
            {
                await task;
            }
            return mediaProperties;
        }

        public GlobalSystemMediaTransportControlsSession Session => session;

        public double PlaybackRate { get; private set; }

        public TimeSpan StartTime => TimeSpan.Zero;

        public TimeSpan EndTime => endTime;

        public TimeSpan Position
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                var v = position + TimeSpan.FromMilliseconds(600);
                return v > endTime ? endTime : v;
            }
        }

        public MediaSessionPlaybackStatus PlaybackStatus { get; private set; }

        public string AppUserModelId => appUserModelId;


        public ICommand PlayCommand => playCommand;
        public ICommand PauseCommand => pauseCommand;
        public ICommand SkipPreviousCommand => skipPreviousCommand;
        public ICommand SkipNextCommand => skipNextCommand;

        public MediaSessionApp App { get; }

        public Task<string?> GetSessionNameAsync()
        {
            if (!string.IsNullOrEmpty(App.CustomName)) return Task.FromResult<string?>(App.CustomName);

            return Task.FromResult<string?>("YesPlayerMusic");
        }

        public Task<ImageSource?> GetSessionIconAsync()
        {
            return Task.FromResult(App.CustomAppIcon);
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

                    session.MediaPropertiesChanged -= Session_MediaPropertiesChanged;
                    session.PlaybackInfoChanged -= Session_PlaybackInfoChanged;

                    mediaProperties = null;
                    playbackInfo = null;

                    endTime = TimeSpan.Zero;
                    position = TimeSpan.Zero;

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


        private class PlayerInfo
        {
            public CurrentTrack? currentTrack { get; set; }

            public double progress { get; set; }

            public class CurrentTrack
            {
                public string? name { get; set; }

                public int id { get; set; }

                public double dt { get; set; }

                public Artist[]? ar { get; set; }

                public Album? al { get; set; }
            }

            public class Artist
            {
                public int id { get; set; }
                public string? name { get; set; }
            }


            public class Album
            {
                public int id { get; set; }
                public string? name { get; set; }
            }

        }
    }
}
