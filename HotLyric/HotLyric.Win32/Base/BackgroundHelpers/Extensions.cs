using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace HotScreen.App.BackgroundHelpers
{
    public static class Extensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static System.Windows.Media.Color ToWpfColor(this Windows.UI.Color color)
        {
            return System.Windows.Media.Color.FromArgb(color.A, color.R, color.G, color.B);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Windows.UI.Color ToWinRTColor(this System.Windows.Media.Color color)
        {
            return Windows.UI.Color.FromArgb(color.A, color.R, color.G, color.B);
        }

        internal static bool IsShadowSupported => Environment.OSVersion.Version >= new Version(10, 0, 18362, 0);
    }
}
