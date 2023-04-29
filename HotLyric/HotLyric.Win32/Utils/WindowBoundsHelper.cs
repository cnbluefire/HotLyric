using Newtonsoft.Json.Linq;
using System;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using Vanara.PInvoke;
using Windows.Foundation;
using Windows.Storage;

namespace HotLyric.Win32.Utils
{
    internal static class WindowBoundsHelper
    {
        private static Rect? windowBounds;

        public static bool TryGetWindowBounds(string key, out double x, out double y, out double width, out double height)
        {
            if (windowBounds.HasValue)
            {
                x = windowBounds.Value.X;
                y = windowBounds.Value.Y;
                width = windowBounds.Value.Width;
                height = windowBounds.Value.Height;

                return true;
            }

            x = 0;
            y = 0;
            width = 0;
            height = 0;

            if (ApplicationData.Current.LocalSettings.Values.TryGetValue($"WindowBounds2_{key}", out var _bounds)
                && _bounds is string bounds)
            {
                var arr = bounds.Split('|')
                    .Select(c =>
                    {
                        if (double.TryParse(c, CultureInfo.InvariantCulture, out var result))
                        {
                            return result;
                        }
                        return 0;
                    }).ToArray();

                x = arr[0];
                y = arr[1];
                width = arr[2];
                height = arr[3];

                windowBounds = new Rect(x, y, width, height);

                return true;
            }
            else if (ApplicationData.Current.LocalSettings.Values.TryGetValue($"WindowBounds_{key}", out var oldBounds)
                && oldBounds is string json)
            {
                var jobj = JObject.Parse(json);
                x = (double)(jobj["x"]!);
                y = (double)(jobj["y"]!);
                width = (double)(jobj["width"]!);
                height = (double)(jobj["height"]!);

                windowBounds = new Rect(x, y, width, height);

                FormattableString formatString = $"{x}|{y}|{width}|{height}";

                ApplicationData.Current.LocalSettings.Values[$"WindowBounds2_{key}"] = formatString.ToString(CultureInfo.InvariantCulture);

                return true;
            }

            return false;
        }

        public static void SetWindowBounds(string key, double x, double y, double width, double height)
        {
            FormattableString formatString = $"{x}|{y}|{width}|{height}";

            ApplicationData.Current.LocalSettings.Values[$"WindowBounds2_{key}"] = formatString.ToString(CultureInfo.InvariantCulture);

            windowBounds = new Rect(x, y, width, height);
        }

        public static void ResetWindowBounds(IntPtr hwnd)
        {
            if (User32.GetWindowRect(hwnd, out var _windowRect))
            {
                var windowRect = (System.Drawing.Rectangle)_windowRect;

                var monitorInfo = new User32.MONITORINFO()
                {
                    cbSize = (uint)Marshal.SizeOf<User32.MONITORINFO>()
                };
                var monitor = User32.MonitorFromPoint(new POINT(0, 0), User32.MonitorFlags.MONITOR_DEFAULTTOPRIMARY);
                User32.GetMonitorInfo(monitor, ref monitorInfo);

                var primaryWorkarea = (System.Drawing.Rectangle)monitorInfo.rcWork;

                var newWindowRect = System.Drawing.Rectangle.Empty;

                if (!monitor.IsNull
                    && SHCore.GetDpiForMonitor(monitor, SHCore.MONITOR_DPI_TYPE.MDT_EFFECTIVE_DPI, out var dpiX, out var dpiY).Succeeded)
                {
                    var oldDpi = User32.GetDpiForWindow(hwnd);

                    if (oldDpi < 90)
                    {
                        oldDpi = 96;
                    }
                    var newSize = new System.Drawing.Size((int)(windowRect.Width * 1.0 / oldDpi * dpiY), (int)(windowRect.Height * 1.0 / oldDpi * dpiY));
                    newWindowRect = new System.Drawing.Rectangle(primaryWorkarea.Left + 20, primaryWorkarea.Bottom - 20 - newSize.Height, newSize.Width, newSize.Height);
                }
                else
                {
                    newWindowRect = new System.Drawing.Rectangle(primaryWorkarea.Left + 20, primaryWorkarea.Bottom - 100, windowRect.Width, windowRect.Height);
                }

                if (newWindowRect.X < primaryWorkarea.Left)
                {
                    newWindowRect.X = primaryWorkarea.Left;
                }

                if (newWindowRect.Y < primaryWorkarea.Top)
                {
                    newWindowRect.Y = primaryWorkarea.Top;
                }

                User32.SetWindowPos(hwnd, IntPtr.Zero, newWindowRect.Left, newWindowRect.Top, newWindowRect.Width, newWindowRect.Height, User32.SetWindowPosFlags.SWP_NOZORDER);
            }
        }

        public static bool IsWindowOutsideScreen(IntPtr hwnd)
        {
            var monitor = User32.MonitorFromWindow(hwnd, User32.MonitorFlags.MONITOR_DEFAULTTONULL);
            if (monitor.IsNull) return true;

            var info = User32.MONITORINFO.Default;

            if (User32.GetMonitorInfo(monitor, ref info)
                && User32.GetWindowRect(hwnd, out var windowRect)
                && SHCore.GetDpiForMonitor(monitor, SHCore.MONITOR_DPI_TYPE.MDT_EFFECTIVE_DPI, out var dpiX, out var dpiY).Succeeded)
            {
                var screenBounds = (System.Drawing.Rectangle)info.rcMonitor;
                var windowBounds = (System.Drawing.Rectangle)windowRect;

                var windowPadding = 10 * (int)dpiX / 96;

                if (windowBounds.Width > windowPadding * 2 && windowBounds.Height > windowPadding * 2)
                {
                    windowBounds.X += windowPadding;
                    windowBounds.Y += windowPadding;

                    windowBounds.Width -= (windowPadding * 2);
                    windowBounds.Height -= (windowPadding * 2);

                    return !screenBounds.IntersectsWith(windowBounds);
                }
            }

            return true;
        }
    }
}
