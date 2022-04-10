using HotLyric.Win32.Utils;
using Kfstorm.LrcParser;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace HotLyric.Win32.Models
{
    public partial class MediaModel : ObservableObject, IDisposable, IEquatable<MediaModel>
    {
        private CancellationTokenSource? cts;

        private ILrcFile? lrcFile;
        private ILrcFile? translatedLrcFile;
        private bool isEmptyLyric;
        private bool disposedValue;
        private bool hasLyric;

        public MediaModel(
            string? name,
            string? artist,
            string? neteaseMusicId,
            string localLrcPath,
            TimeSpan mediaDuration,
            string? defaultProvider,
            bool convertToSimpleChinese)
        {
            Name = name;
            Artist = artist;
            NeteaseMusicId = neteaseMusicId;
            LocalLrcPath = localLrcPath;
            MediaDuration = mediaDuration;
            DefaultProvider = defaultProvider;
            ConvertToSimpleChinese = convertToSimpleChinese;
        }

        public string? Name { get; }

        public string? Artist { get; }

        public string? NeteaseMusicId { get; }

        public string LocalLrcPath { get; }

        public TimeSpan MediaDuration { get; }

        public string? DefaultProvider { get; }

        public bool ConvertToSimpleChinese { get; }

        public ILrcFile? LrcFile
        {
            get => lrcFile;
            private set
            {
                if (SetProperty(ref lrcFile, value))
                {
                    OnPropertyChanged(nameof(HasLyric));
                }
            }
        }

        public ILrcFile? TranslatedLrcFile
        {
            get => translatedLrcFile;
            private set
            {
                if (SetProperty(ref translatedLrcFile, value))
                {
                    OnPropertyChanged(nameof(HasTranslatedLyric));
                }
            }
        }

        public bool HasTranslatedLyric => TranslatedLrcFile != null;

        public bool HasLyric
        {
            get => hasLyric;
            private set => SetProperty(ref hasLyric, value);
        }

        public bool IsEmptyLyric => isEmptyLyric;

        public string DisplayText
        {
            get
            {
                if (string.IsNullOrEmpty(Name)) return string.Empty;
                else if (string.IsNullOrEmpty(Artist)) return Name;
                return $"{Name} - {Artist}";
            }
        }

        public async void StartLoad()
        {
            if (cts != null || LrcFile != null) return;

            if (string.IsNullOrEmpty(Name) && string.IsNullOrEmpty(NeteaseMusicId))
            {
                LrcFile = null;
                return;
            }

            var tmpCts = new CancellationTokenSource();
            cts = tmpCts;

            try
            {
                LrcFile = LrcHelper.DownloadingLyric;

                var file = await LrcHelper.GetLrcFileAsync(LocalLrcPath, tmpCts.Token);

                if (file == null)
                {
                    file = await LrcHelper.GetLrcFileAsync(
                        Name,
                        !string.IsNullOrEmpty(Artist) ? new[] { Artist } : Array.Empty<string>(),
                        !string.IsNullOrWhiteSpace(NeteaseMusicId) ? NeteaseMusicId : "",
                        DefaultProvider,
                        ConvertToSimpleChinese,
                        tmpCts.Token);
                }

                if (file != null)
                {
                    LrcFile = file.Lyric;
                    TranslatedLrcFile = file.Translated;
                    HasLyric = true;
                }
                else
                {
                    LrcFile = null;
                    TranslatedLrcFile = null;
                }
            }
            catch
            {
                LrcFile = null;
            }
        }

        public void Cancel()
        {
            cts?.Cancel();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: 释放托管状态(托管对象)
                    Cancel();
                }
                // TODO: 释放未托管的资源(未托管的对象)并重写终结器
                // TODO: 将大型字段设置为 null
                disposedValue = true;
            }
        }

        // // TODO: 仅当“Dispose(bool disposing)”拥有用于释放未托管资源的代码时才替代终结器
        // ~MediaModel()
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

        public bool Equals(MediaModel? other)
        {
            return other != null
                && other.disposedValue == this.disposedValue
                && other.Name == this.Name
                && other.Artist == this.Artist
                && other.NeteaseMusicId == this.NeteaseMusicId;
        }

        public override bool Equals(object? obj)
        {
            return obj is MediaModel model
                && model.Equals(this);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Name, Artist, NeteaseMusicId);
        }

        public static bool operator ==(MediaModel? left, MediaModel? right)
        {
            if (left is null && right is null) return true;
            else if (left is null || right is null) return false;
            return left.Equals(right);
        }

        public static bool operator !=(MediaModel? left, MediaModel? right)
        {
            return !(left == right);
        }


        public static MediaModel CreateEmptyMedia()
        {
            return new MediaModel("", "", "", "", TimeSpan.Zero, "", false)
            {
                LrcFile = LrcHelper.EmptyLyric,
                isEmptyLyric = true
            };
        }
    }
}
