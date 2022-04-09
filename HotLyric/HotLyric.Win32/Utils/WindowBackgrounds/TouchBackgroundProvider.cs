using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;

namespace HotLyric.Win32.Utils.WindowBackgrounds
{
    internal class TouchBackgroundProvider : WindowBackgroundProvider
    {
        private readonly Window window;

        public TouchBackgroundProvider(Window window) : base(window)
        {
            this.window = window;
            IsHitTestVisible = true;
        }

        protected override void DisposeCore()
        {

        }
    }
}
