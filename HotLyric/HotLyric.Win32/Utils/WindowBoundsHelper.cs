using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
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

    }
}
