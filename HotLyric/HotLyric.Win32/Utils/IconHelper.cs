using HotLyric.Win32.Base.BackgroundHelpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Vanara.PInvoke;

namespace HotLyric.Win32.Utils
{
    internal static class IconHelper
    {
        public static double GetPrimaryDisplayDpiScale()
        {
            using (var dc = User32.GetDC(0))
            {
                return Gdi32.GetDeviceCaps(dc, Gdi32.DeviceCap.LOGPIXELSX) / 96d;
            }
        }

        public static System.Drawing.Size GetIconSize()
        {
            var width = User32.GetSystemMetrics(User32.SystemMetric.SM_CXICON);
            var height = User32.GetSystemMetrics(User32.SystemMetric.SM_CYICON);

            return new System.Drawing.Size(width, height);
        }


        public static System.Drawing.Size GetSmallIconSize()
        {
            var width = User32.GetSystemMetrics(User32.SystemMetric.SM_CXSMICON);
            var height = User32.GetSystemMetrics(User32.SystemMetric.SM_CYSMICON);

            return new System.Drawing.Size(width, height);
        }

        public static System.Drawing.Icon CreateIcon(this System.Drawing.Bitmap bitmap, System.Drawing.Size size, double dpiScale = 1d)
        {
            nint hIcon = 0;
            try
            {
                var sizeWithDpi = new System.Drawing.Size(
                    ((int)(size.Width * dpiScale)),
                    ((int)(size.Height * dpiScale)));

                using var scaledBitmap = new System.Drawing.Bitmap(bitmap, sizeWithDpi);

                hIcon = scaledBitmap.GetHicon();
                using var icon = System.Drawing.Icon.FromHandle(hIcon);

                // https://stackoverflow.com/questions/30979653/icon-fromhandle-should-i-dispose-it-or-call-destroyicon
                // 官方文档含有歧义
                // Icon.FromHandle 方法仍会共享 hIcon 而非复制，但 Dispose 时不会释放 hIcon
                // 所以此处 Clone 一个 Dispose 能正常释放句柄的对象来返回

                return (System.Drawing.Icon)icon.Clone();
            }
            finally
            {
                User32.DestroyIcon(hIcon);
            }
        }

        public static System.Drawing.Icon CreateIcon(string fileName, System.Drawing.Size size, double dpiScale = 1d)
        {
            using (var bitmap = new System.Drawing.Bitmap(fileName))
            {
                return CreateIcon(bitmap, size, dpiScale);
            }
        }

        public static async Task<System.Drawing.Icon> CreateIconAsync(Uri uri, System.Drawing.Size size, double dpiScale, CancellationToken cancellationToken)
        {
            using var stream = await UriResourceHelper.GetStreamAsync(uri, cancellationToken);
            var readStream = stream.AsStreamForRead();

            using var bitmap = new System.Drawing.Bitmap(readStream);

            return bitmap.CreateIcon(size, dpiScale);
        }

        public static Microsoft.UI.IconId GetIconId(this System.Drawing.Icon icon) =>
            new Microsoft.UI.IconId(unchecked((ulong)icon.Handle.ToInt64()));
    }
}
