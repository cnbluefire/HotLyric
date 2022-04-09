using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;

namespace HotLyric.Win32.Utils
{
    public interface IHostWindow
    {
        public Window? ChildWindow { get; }
    }
}
