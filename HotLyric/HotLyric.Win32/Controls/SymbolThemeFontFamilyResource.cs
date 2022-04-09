using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Media;

namespace HotLyric.Win32.Controls
{
    public class SymbolThemeFontFamilyResource : ResourceDictionary
    {
        public SymbolThemeFontFamilyResource()
        {
            var ver = Environment.OSVersion.Version;
            var minWin11Ver = new Version(10, 0, 22000, 0);
            if (ver >= minWin11Ver)
            {
                // Win11
                this["SymbolThemeFontFamily"] = new FontFamily("Segoe Fluent Icons");
            }
            else
            {
                // Win10
                this["SymbolThemeFontFamily"] = new FontFamily("Segoe MDL2 Assets");
            }
        }
    }
}
