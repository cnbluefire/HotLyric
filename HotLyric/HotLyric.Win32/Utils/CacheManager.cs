using HotLyric.Win32.Base.BackgroundHelpers;
using HotLyric.Win32.Utils.LrcProviders;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Metrics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Streams;

namespace HotLyric.Win32.Utils
{
    internal static class ImageCacheHelper
    {
        public static ImageSource GetImage(string uri)
        {
            var dispatcher = DispatcherQueue.GetForCurrentThread();

            var image = new BitmapImage();

            var weakImage = new WeakReference<BitmapImage>(image);
            dispatcher.TryEnqueue(DispatcherQueuePriority.Low, async () => await SetupImageAsync(weakImage, uri));

            return image;
        }

        private static async Task SetupImageAsync(WeakReference<BitmapImage> weakImage, string uri)
        {
            var md5 = LrcProviderHelper.GetMD5(uri);
            var bytes = await CacheManager.GetCacheAsync(md5);

            IRandomAccessStream? stream = null;

            if (bytes != null)
            {
                stream = new MemoryStream(bytes).AsRandomAccessStream();
            }
            else if (Uri.TryCreate(uri, UriKind.Absolute, out var _uri))
            {
                try
                {
                    stream = await UriResourceHelper.GetStreamAsync(_uri, default);
                    stream.Seek(0);
                    using (var memoryOwner = MemoryPool<byte>.Shared.Rent((int)stream.Size))
                    {
                        var memory = memoryOwner.Memory.Slice(0, (int)stream.Size);
                        var readStream = stream.AsStreamForRead();
                        var count = 0;
                        while (count < (int)stream.Size)
                        {
                            count += await readStream.ReadAsync(memory);
                        }
                        await CacheManager.SetCacheAsync(md5, memory);
                        stream.Seek(0);
                    }
                }
                catch { }
            }

            if (stream != null && weakImage.TryGetTarget(out var image))
            {
                await image.SetSourceAsync(stream);
            }
        }
    }

    internal static class CacheManager
    {
        private static readonly IReadOnlyCollection<char> invalidFileNameChars = new HashSet<char>(System.IO.Path.GetInvalidFileNameChars().Distinct());
        private readonly static string cacheFolderPath = System.IO.Path.Combine(ApplicationData.Current.TemporaryFolder.Path, "cache-files");
        private static SemaphoreSlim cacheLocker = new SemaphoreSlim(1, 1);

        public static async Task ClearCacheAsync(CancellationToken cancellationToken = default)
        {
            await Task.Run(async () =>
            {
                await cacheLocker.WaitAsync(cancellationToken);
                try
                {
                    try
                    {
                        if (System.IO.Directory.Exists(cacheFolderPath))
                        {
                            System.IO.Directory.Delete(cacheFolderPath, true);
                        }
                    }
                    catch { }
                }
                finally
                {
                    cacheLocker.Release();
                }
            }, cancellationToken).ConfigureAwait(false);
        }

        public static async Task RotationAsync(DateTime lastAccessTime, CancellationToken cancellationToken = default)
        {
            await Task.Run(async () =>
            {
                await cacheLocker.WaitAsync(cancellationToken);
                try
                {
                    try
                    {
                        if (Directory.Exists(cacheFolderPath))
                        {
                            var files = Directory.GetFiles(cacheFolderPath);
                            for (int i = 0; i < files.Length; i++)
                            {
                                cancellationToken.ThrowIfCancellationRequested();
                                try
                                {
                                    if (File.Exists(files[i]) && File.GetLastAccessTimeUtc(files[i]) < lastAccessTime)
                                    {
                                        File.Delete(files[i]);
                                    }
                                }
                                catch { }
                            }
                        }
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException) { }
                }
                finally
                {
                    cacheLocker.Release();
                }
            }, cancellationToken).ConfigureAwait(false);
        }

        private static void DeleteOldCacheFolder()
        {
            try
            {
                var oldCacheFolder = System.IO.Path.Combine(ApplicationData.Current.TemporaryFolder.Path, "cache-files");
                if (System.IO.Directory.Exists(oldCacheFolder))
                {
                    System.IO.Directory.Delete(oldCacheFolder, true);
                }
            }
            catch { }
        }

        public static async Task<bool> SetCacheAsync(string key, ReadOnlyMemory<char> value, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (IsValidKey(key, out var key2))
            {
                var maxCount = Encoding.UTF8.GetMaxByteCount(value.Length);
                using (var memoryOwner = MemoryPool<byte>.Shared.Rent(maxCount))
                {
                    var length = Encoding.UTF8.GetBytes(value.Span, memoryOwner.Memory.Span);
                    var memory = memoryOwner.Memory.Slice(0, length);
                    return await SetCacheAsyncCore(key2, memory, cancellationToken).ConfigureAwait(false);
                }
            }
            return false;
        }

        public static async Task<bool> SetCacheAsync(string key, ReadOnlyMemory<byte> value, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (IsValidKey(key, out var key2))
            {
                return await SetCacheAsyncCore(key2, value, cancellationToken).ConfigureAwait(false);
            }
            return false;
        }

        public static async Task<byte[]?> GetCacheAsync(string key, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (IsValidKey(key, out var key2))
            {
                return await GetCacheAsyncCore(key2, cancellationToken).ConfigureAwait(false);
            }
            return null;
        }

        public static async Task<string?> GetCacheTextAsync(string key, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (IsValidKey(key, out var key2))
            {
                var bytes = await GetCacheAsyncCore(key2, cancellationToken).ConfigureAwait(false);
                if (bytes == null || bytes.Length == 0) return null;

                try
                {
                    var maxCount = Encoding.UTF8.GetMaxCharCount(bytes.Length);
                    using (var memoryOwner = MemoryPool<char>.Shared.Rent(maxCount))
                    {
                        if (Encoding.UTF8.TryGetChars(bytes, memoryOwner.Memory.Span, out var length))
                        {
                            return new string(memoryOwner.Memory.Span.Slice(0, length));
                        }
                    }
                }
                catch { }
            }
            return null;
        }
        private static bool IsValidKey(string key, [NotNullWhen(true)] out string? normalizedKey)
        {
            normalizedKey = null;
            if (string.IsNullOrWhiteSpace(key)) return false;

            if (key.Length > 240) return false;

            normalizedKey = key.Trim().ToLowerInvariant();

            for (int i = 0; i < normalizedKey.Length; i++)
            {
                if (invalidFileNameChars.Contains(normalizedKey[i]))
                {
                    normalizedKey = null;
                    return false;
                }
            }

            return true;
        }

        private static async Task<bool> SetCacheAsyncCore(string key, ReadOnlyMemory<byte> value, CancellationToken cancellationToken = default)
        {
            return await Task.Run(async () =>
            {
                await cacheLocker.WaitAsync(cancellationToken);
                try
                {
                    var filePath = System.IO.Path.Combine(cacheFolderPath, key);
                    if (value.IsEmpty)
                    {
                        if (System.IO.File.Exists(filePath))
                        {
                            System.IO.File.Delete(filePath);
                            return true;
                        }
                    }
                    else
                    {
                        if (!Directory.Exists(cacheFolderPath)) Directory.CreateDirectory(cacheFolderPath);
                        using (var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None))
                        {
                            await fileStream.WriteAsync(value).ConfigureAwait(false);
                            fileStream.SetLength(fileStream.Position);
                            await fileStream.FlushAsync().ConfigureAwait(false);
                        }
                        var time = DateTime.UtcNow;
                        File.SetLastAccessTimeUtc(filePath, time);
                        File.SetLastWriteTimeUtc(filePath, time);

                        return true;
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    LogHelper.LogError(ex);
                }
                finally
                {
                    cacheLocker.Release();
                }
                return false;
            }, cancellationToken).ConfigureAwait(false);
        }

        private static async Task<byte[]?> GetCacheAsyncCore(string key, CancellationToken cancellationToken)
        {
            return await Task.Run(async () =>
            {
                await cacheLocker.WaitAsync(cancellationToken);
                try
                {
                    var filePath = System.IO.Path.Combine(cacheFolderPath, key);
                    if (System.IO.File.Exists(filePath))
                    {
                        var bytes = await File.ReadAllBytesAsync(filePath).ConfigureAwait(false);
                        if (bytes != null && bytes.Length > 0)
                        {
                            File.SetLastAccessTimeUtc(filePath, DateTime.UtcNow);
                            return bytes;
                        }
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    LogHelper.LogError(ex);
                }
                finally
                {
                    cacheLocker.Release();
                }
                return null;
            }, cancellationToken).ConfigureAwait(false);
        }
    }
}
