using HotLyric.Win32.Utils;
using HotLyric.Win32.Utils.SystemMediaTransportControls;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Windows.Media.Control;

namespace HotLyric.Win32.Models
{
    public partial class SMTCSessionModel : ObservableObject, IDisposable
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

        public SMTCSession Session { get; private set; }
        private GlobalSystemMediaTransportControlsSessionMediaProperties? mediaProperties;
        private string? neteaseMusicId;
        private string? localLrcPath;

        private SMTCSessionModel(SMTCSession session)
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

        public bool IsPlaying => Session.PlaybackStatus == GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing;

        public bool IsPlayButtonVisible => !IsPlaying && Session.PlayCommand.CanExecute(null);

        public bool IsPauseButtonVisible => IsPlaying && Session.PauseCommand.CanExecute(null);

        public bool IsPreviousButtonVisible => Session.SkipPreviousCommand.CanExecute(null);

        public bool IsNextButtonVisible => Session.SkipNextCommand.CanExecute(null);

        public double PlaybackRate => Session.PlaybackRate;

        public string MediaTitle => mediaProperties?.Title ?? string.Empty;

        public string MediaArtist => mediaProperties?.Artist ?? string.Empty;

        public string NeteaseMusicId
        {
            get
            {
                if (neteaseMusicId == null)
                {
                    var genres = mediaProperties?.Genres?.ToArray();
                    if (genres != null && genres.Length > 0 && genres[0]?.StartsWith("ncm-", StringComparison.OrdinalIgnoreCase) == true)
                    {
                        neteaseMusicId = genres[0].Substring(4);
                    }
                }
                if (neteaseMusicId == null) neteaseMusicId = string.Empty;
                return neteaseMusicId;
            }
        }

        public string LocalLrcPath
        {
            get
            {
                if (localLrcPath == null)
                {
                    var genres = mediaProperties?.Genres?.ToArray();
                    if (genres != null
                        && genres.Length > 1
                        && !string.IsNullOrEmpty(genres[1])
                        && genres[1].Trim() is string path
                        && System.IO.Path.IsPathFullyQualified(path))
                    {
                        localLrcPath = path;
                    }
                }
                if (localLrcPath == null) localLrcPath = string.Empty;
                return localLrcPath;
            }
        }

        public MediaModel CreateMediaModel()
        {
            return new MediaModel(MediaTitle, MediaArtist, NeteaseMusicId, LocalLrcPath, Session.EndTime);
        }

        private async void Session_MediaPropertiesChanged(object? sender, EventArgs e)
        {
            mediaProperties = await Session.GetMediaPropertiesAsync();

            neteaseMusicId = null;

            await DispatcherHelper.UIDispatcher!.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(() =>
            {
                OnPropertyChanged(mediaTitleChangedArgs);
                OnPropertyChanged(mediaArtistChangedArgs);
                OnPropertyChanged(neteaseMusicIdChangedArgs);

                MediaChanged?.Invoke(this, EventArgs.Empty);
            }));
        }


        private void Session_PlaybackInfoChanged(object? sender, EventArgs e)
        {
            _ = DispatcherHelper.UIDispatcher!.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(() =>
            {
                OnPropertyChanged(positionChangedArgs);
                OnPropertyChanged(isPlayingChangedArgs);
                OnPropertyChanged(playbackRateChangedArgs);

                RaiseCommandButtonVisibleChanged();
            }));
        }

        private void RaiseCommandButtonVisibleChanged()
        {
            OnPropertyChanged(isPlayButtonVisibleChangedArgs);
            OnPropertyChanged(isPauseButtonVisibleChangedArgs);
            OnPropertyChanged(isPreviousButtonVisibleChangedArgs);
            OnPropertyChanged(isNextButtonVisibleChangedArgs);
        }

        public event EventHandler? MediaChanged;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: 释放托管状态(托管对象)
                    Session.PlaybackInfoChanged -= Session_PlaybackInfoChanged;
                    Session.MediaPropertiesChanged -= Session_MediaPropertiesChanged;

                    Session.Dispose();
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

        public static async Task<SMTCSessionModel?> CreateAsync(SMTCSession session)
        {
            if (session == null) return null;

            ImageSource? image = session.CustomAppIcon;
            string? title = session.CustomName;

            if (image == null && string.IsNullOrEmpty(title))
            {
                var package = await session.GetAppPackageAsync();
                if (package != null)
                {
                    if (string.IsNullOrEmpty(title))
                    {
                        title = package.DisplayName ?? string.Empty;
                    }

                    if (image == null)
                    {
                        image = await ApplicationHelper.GetPackageIconAsync(package);
                    }
                }
            }

            var mediaProperties = await session.GetMediaPropertiesAsync();

            return new SMTCSessionModel(session)
            {
                appTitle = title,
                appIcon = image,
                mediaProperties = mediaProperties,
            };
        }
    }
}
