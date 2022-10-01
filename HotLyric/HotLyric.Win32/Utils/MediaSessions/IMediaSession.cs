using HotLyric.Win32.Utils.MediaSessions;
using System;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media;
using Windows.ApplicationModel;
using Windows.Media.Control;

namespace HotLyric.Win32.Utils.MediaSessions
{
    public interface IMediaSession : IDisposable
    {
        MediaSessionApp App { get; }

        TimeSpan EndTime { get; }

        ICommand PauseCommand { get; }
        ICommand PlayCommand { get; }
        ICommand SkipNextCommand { get; }
        ICommand SkipPreviousCommand { get; }

        MediaSessionPlaybackStatus PlaybackStatus { get; }

        double PlaybackRate { get; }

        TimeSpan Position { get; }

        TimeSpan StartTime { get; }

        Task<MediaSessionMediaProperties?> GetMediaPropertiesAsync();

        event EventHandler? MediaPropertiesChanged;

        event EventHandler? PlaybackInfoChanged;

        Task<string?> GetSessionNameAsync();

        Task<ImageSource?> GetSessionIconAsync();

        public bool IsDisposed { get; }
    }
}