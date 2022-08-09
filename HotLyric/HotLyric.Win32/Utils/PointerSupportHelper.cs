using System;
using System.Collections.Generic;
using System.Text;

namespace HotLyric.Win32.Utils
{
    public static class PointerSupportHelper
    {
        public static bool IsPointerSupported
        {
            get
            {
                if (!Windows.Storage.ApplicationData.Current.LocalSettings.Values.TryGetValue("IsPointerSupported", out var _ps))
                {
                    _ps = null;
                }
                return (string?)_ps != "false";
            }
            set
            {
                Windows.Storage.ApplicationData.Current.LocalSettings.Values["IsPointerSupported"] = value ? "true" : "false";
            }
        }

        public static void Initialize()
        {
            if (IsPointerSupported)
            {
                AppContext.SetSwitch("Switch.System.Windows.Input.Stylus.EnablePointerSupport", true);
            }
        }
    }
}
