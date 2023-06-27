using HotLyric.Win32.Utils;
using HotLyric.Win32.Utils.MediaSessions;
using HotLyric.Win32.Utils.MediaSessions.SMTC;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Windows.Media.Control;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;

namespace HotLyric.Win32.Models
{
    public partial class MediaSessionModel : ObservableObject, IDisposable
    {
        private static readonly PropertyChangedEventArgs positionChangedArgs = new PropertyChangedEventArgs(nameof(Position));
        private static readonly PropertyChangedEventArgs isPlayingChangedArgs = new PropertyChangedEventArgs(nameof(IsPlaying));
        private static readonly PropertyChangedEventArgs playbackRateChangedArgs = new PropertyChangedEventArgs(nameof(PlaybackRate));

        private static readonly PropertyChangedEventArgs mediaTitleChangedArgs = new PropertyChangedEventArgs(nameof(MediaTitle));
        private static readonly PropertyChangedEventArgs mediaArtistChangedArgs = new PropertyChangedEventArgs(nameof(MediaArtist));
        private static readonly PropertyChangedEventArgs neteaseMusicIdChangedArgs = new PropertyChangedEventArgs(nameof(NeteaseMusicId));

        private static readonly PropertyChangedEventArgs isPlayButtonVisibleChangedArgs = new PropertyChangedEventArgs(nameof(IsPlayButtonVisible));
        private static readonly PropertyChangedEventArgs isPauseButtonVisibleChangedArgs = new PropertyChangedEventArgs(nameof(IsPauseButtonVisible));
        private static readonly PropertyChangedEventArgs isPreviousButtonVisibleChangedArgs = new PropertyChangedEventArgs(nameof(IsPreviousButtonVisible));
        private static readonly PropertyChangedEventArgs isNextButtonVisibleChangedArgs = new PropertyChangedEventArgs(nameof(IsNextButtonVisible));

        private bool disposedValue;

        public IMediaSession Session { get; private set; }
        private MediaSessionMediaProperties? mediaProperties;

        private MediaSessionModel(IMediaSession session)
        {
            Session = session;
            Session.PlaybackInfoChanged += Session_PlaybackInfoChanged;
            Session.MediaPropertiesChanged += Session_MediaPropertiesChanged;
        }

        private string? appTitle;

        private ImageSource? appIcon;

        public string? AppTitle
        {
            get => appTitle;
            private set => SetProperty(ref appTitle, value);
        }

        public ImageSource? AppIcon
        {
            get => appIcon;
            private set => SetProperty(ref appIcon, value);
        }

        public TimeSpan Position => Session.Position;

        public bool IsPlaying => Session.PlaybackStatus == MediaSessionPlaybackStatus.Playing;

        public bool IsPlayButtonVisible => !IsPlaying && Session.PlayCommand.CanExecute(null);

        public bool IsPauseButtonVisible => IsPlaying && Session.PauseCommand.CanExecute(null);

        public bool IsPreviousButtonVisible => Session.SkipPreviousCommand.CanExecute(null);

        public bool IsNextButtonVisible => Session.SkipNextCommand.CanExecute(null);

        public double PlaybackRate => Session.PlaybackRate;

        public string MediaTitle => mediaProperties?.Title ?? string.Empty;

        public string MediaArtist => mediaProperties?.Artist ?? string.Empty;

        public string NeteaseMusicId => mediaProperties?.NeteaseMusicId ?? string.Empty;

        public string LocalLrcPath => mediaProperties?.LocalLrcPath ?? string.Empty;

        public MediaModel CreateMediaModel()
        {
            if (string.IsNullOrEmpty(MediaTitle))
            {
                return MediaModel.CreateEmptyMedia();
            }

            return new MediaModel(MediaTitle, MediaArtist, NeteaseMusicId, LocalLrcPath, Session.EndTime, Session.App.DefaultLrcProvider, Session.App.ConvertToSimpleChinese);
        }

        private async void Session_MediaPropertiesChanged(object? sender, EventArgs e)
        {
            mediaProperties = await Session.GetMediaPropertiesAsync();

            App.DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Normal, () =>
            {
                OnPropertyChanged(mediaTitleChangedArgs);
                OnPropertyChanged(mediaArtistChangedArgs);
                OnPropertyChanged(neteaseMusicIdChangedArgs);

                MediaChanged?.Invoke(this, EventArgs.Empty);
            });
        }


        private void Session_PlaybackInfoChanged(object? sender, EventArgs e)
        {
            App.DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Normal, () =>
            {
                OnPropertyChanged(positionChangedArgs);
                OnPropertyChanged(isPlayingChangedArgs);
                OnPropertyChanged(playbackRateChangedArgs);

                RaiseCommandButtonVisibleChanged();

                PlaybackInfoChanged?.Invoke(this, EventArgs.Empty);
            });
        }

        private void RaiseCommandButtonVisibleChanged()
        {
            OnPropertyChanged(isPlayButtonVisibleChangedArgs);
            OnPropertyChanged(isPauseButtonVisibleChangedArgs);
            OnPropertyChanged(isPreviousButtonVisibleChangedArgs);
            OnPropertyChanged(isNextButtonVisibleChangedArgs);
        }

        public event EventHandler? MediaChanged;
        public event EventHandler? PlaybackInfoChanged;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: 释放托管状态(托管对象)
                    Session.PlaybackInfoChanged -= Session_PlaybackInfoChanged;
                    Session.MediaPropertiesChanged -= Session_MediaPropertiesChanged;

                    Session = null!;
                }

                // TODO: 释放未托管的资源(未托管的对象)并重写终结器
                // TODO: 将大型字段设置为 null
                disposedValue = true;
            }
        }

        // // TODO: 仅当“Dispose(bool disposing)”拥有用于释放未托管资源的代码时才替代终结器
        // ~SMTCSessionModel()
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

        public static async Task<MediaSessionModel?> CreateAsync(IMediaSession session)
        {
            if (session == null) return null;

            var image = await session.GetSessionIconAsync();
            var title = (await session.GetSessionNameAsync()) ?? string.Empty;

            MediaSessionMediaProperties? mediaProperties = null;
            for (int i = 0; i < 5; i++)
            {
                mediaProperties = await session.GetMediaPropertiesAsync();
                if (mediaProperties != null) break;
                await Task.Delay(50);
            }

            return new MediaSessionModel(session)
            {
                appTitle = title,
                appIcon = image ?? new BitmapImage(new Uri("ms-appx:///Assets/HotLyricIcon.png")),
                mediaProperties = mediaProperties,
            };
        }
    }
}
