using BlueFire.Toolkit.WinUI3.Extensions;
using BlueFire.Toolkit.WinUI3.SystemBackdrops;
using BlueFire.Toolkit.WinUI3.WindowBase;
using HotLyric.Win32.Utils;
using Microsoft.UI;
using Microsoft.UI.Composition;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Vanara.PInvoke;
using Windows.UI;
using WinRT;

namespace HotLyric.Win32.Base
{
    public class TransparentWindow : WindowEx
    {
        public TransparentWindow()
        {
            var handle = AppWindow.GetWindowHandle();

            var style = (uint)User32.GetWindowLongAuto(handle, User32.WindowLongFlags.GWL_STYLE).ToInt64();
            var exStyle = (uint)User32.GetWindowLongAuto(handle, User32.WindowLongFlags.GWL_EXSTYLE).ToInt64();
            UpdateStyleValue(ref style, ref exStyle);
            User32.SetWindowLong(handle, User32.WindowLongFlags.GWL_STYLE, (nint)style);
            User32.SetWindowLong(handle, User32.WindowLongFlags.GWL_EXSTYLE, (nint)exStyle);

            var manager = WindowManager.Get(this.AppWindow);
            manager!.WindowMessageReceived += Manager_WindowMessageReceived;

            this.SystemBackdrop = new TransparentBackdrop();
        }

        private unsafe void Manager_WindowMessageReceived(WindowManager sender, WindowMessageReceivedEventArgs e)
        {
            if (e.MessageId == (uint)User32.WindowMessage.WM_STYLECHANGING)
            {
                var style = ((STYLESTRUCT*)e.LParam.ToPointer())->styleNew;
                var tmp = 0u;

                if (e.WParam.ToUInt64() == unchecked((ulong)User32.WindowLongFlags.GWL_STYLE))
                {
                    UpdateStyleValue(ref style, ref tmp);
                }
                else
                {
                    UpdateStyleValue(ref tmp, ref style);
                }

                ((STYLESTRUCT*)e.LParam.ToPointer())->styleNew = style;

                e.Handled = true;
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct STYLESTRUCT
        {
            public uint styleOld;
            public uint styleNew;
        }

        private static void UpdateStyleValue(ref uint style, ref uint exStyle)
        {
            style &= ~(uint)(User32.WindowStyles.WS_OVERLAPPEDWINDOW | User32.WindowStyles.WS_BORDER);
            style |= (uint)(User32.WindowStyles.WS_POPUP);

            exStyle &= ~(uint)(User32.WindowStylesEx.WS_EX_APPWINDOW);
            exStyle |= (uint)(User32.WindowStylesEx.WS_EX_TOOLWINDOW);
            exStyle |= (uint)(User32.WindowStylesEx.WS_EX_NOACTIVATE);
        }
    }
}
