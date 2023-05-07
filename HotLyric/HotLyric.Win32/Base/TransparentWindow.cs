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
using WinUIEx;

namespace HotLyric.Win32.Base
{
    public class TransparentWindow : WinUIEx.WindowEx
    {
        public TransparentWindow()
        {
            var handle = this.GetWindowHandle();

            SetWindowTransparentStyle(handle);

            DwmApi.DwmExtendFrameIntoClientArea(handle, new DwmApi.MARGINS(-1));
            DwmApi.DwmEnableBlurBehindWindow(handle, new DwmApi.DWM_BLURBEHIND(true));

            var manager = WindowManager.Get(this);
            manager.WindowMessageReceived += Manager_WindowMessageReceived;

            var brushHost = this.As<ICompositionSupportsSystemBackdrop>();
            if (brushHost != null)
            {
                brushHost.SystemBackdrop = WindowsCompositionHelper.Compositor.CreateColorBrush(Color.FromArgb(0, 255, 255, 255));
            }
        }

        private unsafe void Manager_WindowMessageReceived(object? sender, WinUIEx.Messaging.WindowMessageEventArgs e)
        {
            if (e.Message.MessageId == (uint)User32.WindowMessage.WM_STYLECHANGING)
            {
                var style = ((STYLESTRUCT*)e.Message.LParam.ToPointer())->styleNew;
                var tmp = 0u;

                if (e.Message.WParam.ToUInt64() == unchecked((ulong)User32.WindowLongFlags.GWL_STYLE))
                {
                    UpdateStyleValue(ref style, ref tmp);
                }
                else
                {
                    UpdateStyleValue(ref tmp, ref style);
                }

                ((STYLESTRUCT*)e.Message.LParam.ToPointer())->styleNew = style;

                e.Handled = true;
            }
            else if (e.Message.MessageId == (uint)User32.WindowMessage.WM_DISPLAYCHANGE)
            {
                var dpi = this.GetDpiForWindow();
                OnDisplayChanged(dpi);
            }
            else if (e.Message.MessageId == (uint)User32.WindowMessage.WM_DESTROY)
            {
                var manager = WindowManager.Get(this);
                manager.WindowMessageReceived -= Manager_WindowMessageReceived;
            }
        }

        protected virtual void OnDisplayChanged(uint dpi)
        {

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
            exStyle |= (uint)(User32.WindowStylesEx.WS_EX_TOOLWINDOW
                | User32.WindowStylesEx.WS_EX_LAYERED
                | User32.WindowStylesEx.WS_EX_NOACTIVATE);
        }

        public static void SetWindowTransparentStyle(WindowId windowId) =>
            SetWindowTransparentStyle(unchecked((nint)windowId.Value));

        public static void SetWindowTransparentStyle(nint handle)
        {
            uint style = 0;
            uint exStyle = 0;

            try
            {
                style = (uint)User32.GetWindowLongAuto(handle, User32.WindowLongFlags.GWL_STYLE);
                exStyle = (uint)User32.GetWindowLongAuto(handle, User32.WindowLongFlags.GWL_EXSTYLE);
            }
            catch { }

            UpdateStyleValue(ref style, ref exStyle);

            User32.SetWindowLong(handle, User32.WindowLongFlags.GWL_STYLE, (nint)style);
            User32.SetWindowLong(handle, User32.WindowLongFlags.GWL_EXSTYLE, (nint)exStyle);

        }
    }
}
