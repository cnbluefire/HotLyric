using Linearstar.Windows.RawInput;
using System;
using System.Collections.Generic;
using System.Text;
using Vanara.PInvoke;

namespace HotLyric.Input
{
    internal class RawInputWindow : System.Windows.Forms.NativeWindow
    {
        public event EventHandler<RawInputEventArgs>? Input;

        public RawInputWindow()
        {
            CreateHandle(new System.Windows.Forms.CreateParams
            {
                X = 0,
                Y = 0,
                Width = 0,
                Height = 0,
            });
        }

        protected override void WndProc(ref System.Windows.Forms.Message m)
        {
            const int WM_INPUT = 0x00FF;

            if (m.Msg == WM_INPUT)
            {
                var data = RawInputData.FromHandle(m.LParam);

                Input?.Invoke(this, new RawInputEventArgs(data));
            }

            base.WndProc(ref m);
        }
    }

    internal class RawInputEventArgs : EventArgs
    {
        public RawInputEventArgs(RawInputData data)
        {
            Data = data;
        }

        public RawInputData Data { get; }
    }
}
