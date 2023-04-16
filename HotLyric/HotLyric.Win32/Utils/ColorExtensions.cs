using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI;

namespace HotLyric.Win32.Utils
{
    public static class ColorExtensions
    {
        public static Color WithOpacity(this Color color, double opacity)
        {
            opacity = Math.Min(1, Math.Max(0, opacity));

            return Color.FromArgb((byte)(color.A * opacity), color.R, color.G, color.B);
        }

        public static Color CompositeColor(Color color1, Color color2, double progress)
        {
            var a1 = color1.A / 255d * (1 - progress);
            var a2 = color2.A / 255d * progress;
            var _a = a1 + a2 - a1 * a2;

            var a = (byte)Math.Min(_a * 255, 255);
            var r = (byte)Math.Min((color1.R * a1 * (1 - a2) + color2.R * a2) / _a, 255);
            var g = (byte)Math.Min((color1.G * a1 * (1 - a2) + color2.G * a2) / _a, 255);
            var b = (byte)Math.Min((color1.B * a1 * (1 - a2) + color2.B * a2) / _a, 255);

            return Color.FromArgb(a, r, g, b);
        }

    }
}
