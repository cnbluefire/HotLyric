using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage.Streams;

namespace HotLyric.Win32.Base.BackgroundHelpers
{
    internal static class UriResourceHelper
    {
        private static HttpClient? httpClient;

        public static async Task<IRandomAccessStream> GetStreamAsync(Uri uri, CancellationToken cancellationToken)
        {
            switch (uri.Scheme.ToLowerInvariant())
            {
                case "file":
                    {
                        var fileStream = File.OpenRead(uri.LocalPath);
                        return fileStream.AsRandomAccessStream();
                    }

                case "ms-appx":
                case "ms-appdata":
                    {
                        var file = await Windows.Storage.StorageFile.GetFileFromApplicationUriAsync(uri);
                        return await file.OpenReadAsync();
                    }

                case "http":
                case "https":
                case "ftp":
                    {
                        if (httpClient == null)
                        {
                            httpClient = new HttpClient();
                        }

                        using var webStream = await httpClient.GetStreamAsync(uri, cancellationToken);
                        var memoryStream = new MemoryStream();

                        await webStream.CopyToAsync(memoryStream, cancellationToken);

                        return memoryStream.AsRandomAccessStream();
                    }

                default:
                    throw new NotSupportedException(nameof(uri));
            }
        }
    }
}
