using BlueFire.Toolkit.WinUI3.Input;
using HotLyric.Win32.Models;
using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HotLyric.Win32.Utils.Converters
{
    public static class BindUtils
    {
        public static bool BooleanReverse(bool value) => !value;

        public static double OpacityVisible(bool value) => value ? 1 : 0;

        public static string ToFormatString(object value, string format) => string.Format(format, value);

        public static Visibility Visible(bool value) => value ? Visibility.Visible : Visibility.Collapsed;

        public static Visibility NotVisible(bool value) => Visible(!value);

        public static string HotKeyTip(HotKeyModel? hotKeyModel)
        {
            if (hotKeyModel == null) return "";
            return hotKeyModel.ToString();
        }
    }
}
