using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace HotLyric.Win32.Utils
{
    public static class TraditionalChineseHelper
    {
        public static string ConvertToSimpleChinese(string? str)
        {
            if (string.IsNullOrEmpty(str)) return string.Empty;

            var sb = new StringBuilder();
            if (Vanara.PInvoke.Kernel32.LCMapString(2052, (uint)Vanara.PInvoke.Kernel32.LCMAP.LCMAP_SIMPLIFIED_CHINESE, str, str.Length, sb, str.Length) > 0)
            {
                sb.Length = str.Length;
                return sb.ToString();
            }

            return string.Empty;
        }
    }
}
