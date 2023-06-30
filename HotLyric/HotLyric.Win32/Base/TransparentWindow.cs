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
        private static COLORREF WindowBackgroundColor;

        static TransparentWindow()
        {
            WindowBackgroundColor = User32.GetSysColor(SystemColorIndex.COLOR_WINDOW);
        }

        public TransparentWindow()
        {
            var handle = this.GetWindowHandle();

            SetWindowTransparentStyle(handle);

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
            else if (e.Message.MessageId == (uint)User32.WindowMessage.WM_ERASEBKGND)
            {
                if (User32.GetClientRect(e.Message.Hwnd, out var rect))
                {
                    using var brush = Gdi32.CreateSolidBrush(new COLORREF(0, 0, 0));
                    User32.FillRect((nint)e.Message.WParam, rect, brush);
                    e.Result = 1;
                    e.Handled = true;
                }
            }
            else if (e.Message.MessageId == (uint)User32.WindowMessage.WM_DWMCOMPOSITIONCHANGED)
            {
                SetDwmProperties(e.Message.Hwnd);
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
            exStyle |= (uint)(User32.WindowStylesEx.WS_EX_TOOLWINDOW);
            exStyle |= (uint)(User32.WindowStylesEx.WS_EX_LAYERED);
            exStyle |= (uint)(User32.WindowStylesEx.WS_EX_NOACTIVATE);
        }

        public static void SetWindowTransparentStyle(Microsoft.UI.WindowId windowId) =>
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

            if (WindowBackgroundColor.ToArgb() != 0)
            {
                User32.SetLayeredWindowAttributes(handle, WindowBackgroundColor, 255, User32.LayeredWindowAttributes.LWA_COLORKEY);
            }
            else
            {
                User32.SetLayeredWindowAttributes(handle, default, 255, User32.LayeredWindowAttributes.LWA_ALPHA);
            }

            SetDwmProperties(handle);
        }

        private static void SetDwmProperties(nint handle)
        {
            DwmApi.DwmExtendFrameIntoClientArea(handle, new DwmApi.MARGINS(0));
            using var rgn = Gdi32.CreateRectRgn(-2, -2, -1, -1);
            DwmApi.DwmEnableBlurBehindWindow(handle, new DwmApi.DWM_BLURBEHIND(true)
            {
                dwFlags = DwmApi.DWM_BLURBEHIND_Mask.DWM_BB_ENABLE | DwmApi.DWM_BLURBEHIND_Mask.DWM_BB_BLURREGION,
                hRgnBlur = rgn
            });
        }
    }
}
