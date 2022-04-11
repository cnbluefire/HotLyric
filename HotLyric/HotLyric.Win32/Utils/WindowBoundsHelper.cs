using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Vanara.PInvoke;
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

            if (ApplicationData.Current.LocalSettings.Values.TryGetValue($"WindowBounds_{key}", out var bounds) && bounds is string json)
            {
                var jobj = JObject.Parse(json);
                x = (double)(jobj["x"]!);
                y = (double)(jobj["y"]!);
                width = (double)(jobj["width"]!);
                height = (double)(jobj["height"]!);

                windowBounds = new Rect(x, y, width, height);

                return true;
            }

            return false;
        }

        public static void SetWindowBounds(string key, double x, double y, double width, double height)
        {
            var jobj = new JObject();
            jobj["x"] = x;
            jobj["y"] = y;
            jobj["width"] = width;
            jobj["height"] = height;

            ApplicationData.Current.LocalSettings.Values[$"WindowBounds_{key}"] = jobj.ToString();

            windowBounds = new Rect(x, y, width, height);
        }

        public static void ResetWindowBounds(IntPtr hwnd)
        {
            if (User32.GetWindowRect(hwnd, out var _windowRect))
            {
                var windowRect = (System.Drawing.Rectangle)_windowRect;
                var primaryRect = System.Windows.Forms.Screen.PrimaryScreen.Bounds;
                var primaryWorkarea = System.Windows.Forms.Screen.PrimaryScreen.WorkingArea;

                var newWindowRect = System.Drawing.Rectangle.Empty;

                var primaryWindowCenterPoint = new POINT(primaryRect.Left + primaryRect.Width / 2, primaryRect.Top + primaryRect.Height / 2);
                var monitor = User32.MonitorFromPoint(primaryWindowCenterPoint, User32.MonitorFlags.MONITOR_DEFAULTTONULL);

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
    }
}
