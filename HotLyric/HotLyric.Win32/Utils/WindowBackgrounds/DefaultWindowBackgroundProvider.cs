using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;

namespace HotLyric.Win32.Utils.WindowBackgrounds
{
    internal class DefaultWindowBackgroundProvider : WindowBackgroundProvider
    {
        private readonly Window window;
        private WindowBackgroundInputSink inputSink;

        public override bool IsTransparent
        {
            get => inputSink.IsTransparent;
            set => inputSink.IsTransparent = value;
        }

        public DefaultWindowBackgroundProvider(Window window) : base(window)
        {
            this.window = window;
            inputSink = new WindowBackgroundInputSink(window);
            inputSink.MouseStateChanged += InputSink_MouseStateChanged;
        }

        private void InputSink_MouseStateChanged(object? sender, EventArgs e)
        {
            IsHitTestVisible = inputSink.MouseOverWindow;
        }

        protected override void DisposeCore()
        {
            if (inputSink != null)
            {
                inputSink.MouseStateChanged -= InputSink_MouseStateChanged;
                inputSink?.Dispose();
                inputSink = null!;
            }
        }
    }
}
