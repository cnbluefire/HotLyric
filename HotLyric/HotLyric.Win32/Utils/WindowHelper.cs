using HotLyric.Win32.Base;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Vanara.PInvoke;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml;
using Microsoft.UI;
using BlueFire.Toolkit.WinUI3.Extensions;

namespace HotLyric.Win32.Utils
{
    public static class WindowHelper
    {
        private static System.Drawing.Icon? appIcon;
        private static ImageSource? appIconImage;
        private static readonly Dictionary<IntPtr, bool> isWindowOfProcessElevatedCache = new Dictionary<IntPtr, bool>();

        public static void SetTransparent(this Window window, bool value)
        {
            if (window == null) return;

            var hwnd = window.GetWindowHandle();

            if (hwnd == IntPtr.Zero) return;

            var v = (long)User32.GetWindowLongAuto(hwnd, User32.WindowLongFlags.GWL_EXSTYLE);

            if (value)
            {
                v |= (uint)User32.WindowStylesEx.WS_EX_TRANSPARENT;
            }
            else
            {
                v &= ~((uint)User32.WindowStylesEx.WS_EX_TRANSPARENT);
            }

            User32.SetWindowLong(hwnd, User32.WindowLongFlags.GWL_EXSTYLE, (IntPtr)v);
        }

        public static void SetTopmost(this Window window, bool value)
        {
            if (window == null) return;

            var hwnd = window.GetWindowHandle();

            SetTopmost(hwnd, value);
        }

        public static void SetTopmost(IntPtr hwnd, bool value)
        {
            if (hwnd == IntPtr.Zero) return;

            var flag = User32.SetWindowPosFlags.SWP_NOMOVE
                | User32.SetWindowPosFlags.SWP_NOSIZE
                | User32.SetWindowPosFlags.SWP_NOACTIVATE;

            User32.SetWindowPos(hwnd, value ? User32.SpecialWindowHandles.HWND_TOPMOST : User32.SpecialWindowHandles.HWND_NOTOPMOST, 0, 0, 0, 0, flag);
        }

        public static void SetLayeredWindow(Window window, bool value)
        {
            var hwnd = window.GetWindowHandle();
            SetLayeredWindow(hwnd, value);
        }

        public static void SetLayeredWindow(IntPtr hwnd, bool value)
        {
            if (hwnd == IntPtr.Zero) return;

            var v = (long)User32.GetWindowLongAuto(hwnd, User32.WindowLongFlags.GWL_EXSTYLE);

            if (value)
            {
                v |= (uint)User32.WindowStylesEx.WS_EX_LAYERED;
            }
            else
            {
                v &= ~((uint)User32.WindowStylesEx.WS_EX_LAYERED);
            }

            User32.SetWindowLong(hwnd, User32.WindowLongFlags.GWL_EXSTYLE, (IntPtr)v);
        }

        public static void SetWindowIconVisible(IntPtr hwnd, bool value)
        {
            if (hwnd == IntPtr.Zero) return;

            var v = (long)User32.GetWindowLongAuto(hwnd, User32.WindowLongFlags.GWL_EXSTYLE);

            if (value)
            {
                v &= ~((uint)User32.WindowStylesEx.WS_EX_DLGMODALFRAME);
            }
            else
            {
                v |= (uint)User32.WindowStylesEx.WS_EX_DLGMODALFRAME;
            }

            User32.SetWindowLong(hwnd, User32.WindowLongFlags.GWL_EXSTYLE, (IntPtr)v);
            User32.SetWindowPos(hwnd, IntPtr.Zero, 0, 0, 0, 0,
                User32.SetWindowPosFlags.SWP_NOSIZE
                | User32.SetWindowPosFlags.SWP_NOMOVE
                | User32.SetWindowPosFlags.SWP_NOZORDER
                | User32.SetWindowPosFlags.SWP_FRAMECHANGED);
        }

        public static System.Drawing.Icon GetDefaultAppIcon()
        {
            if (appIcon != null) return appIcon;

            var dirInfo = new System.IO.DirectoryInfo(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory!));
            var iconPath = System.IO.Path.Combine(dirInfo.Parent!.FullName, "Images", "Square44x44Logo.scale-200.png");

            using (var bitmap = new System.Drawing.Bitmap(iconPath))
            {
                appIcon = System.Drawing.Icon.FromHandle(bitmap.GetHicon());
            }

            return appIcon!;
        }

        public static ImageSource GetDefaultAppIconImage()
        {
            if (appIconImage != null) return appIconImage;

            var dirInfo = new System.IO.DirectoryInfo(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory!));
            var iconPath = System.IO.Path.Combine(dirInfo.Parent!.FullName, "Images", "Square44x44Logo.scale-200.png");
            using (var stream = System.IO.File.OpenRead(iconPath))
            {
                var ms = new MemoryStream((int)stream.Length);
                stream.CopyTo(ms);
                ms.Flush();
                ms.Position = 0;

                var image = new BitmapImage(new Uri("ms-appx:///Images/Square44x44Logo.scale-200.png"));

                appIconImage = image;
            }

            return appIconImage;
        }

        public static void SetToolWindowStyle(this Window window, bool value)
        {
            if (window == null) return;

            var hwnd = window.GetWindowHandle();

            if (hwnd == IntPtr.Zero) return;

            var v = (long)User32.GetWindowLongAuto(hwnd, User32.WindowLongFlags.GWL_EXSTYLE);

            if (value)
            {
                v |= (uint)(User32.WindowStylesEx.WS_EX_TOOLWINDOW);
            }
            else
            {
                v &= ~(uint)(User32.WindowStylesEx.WS_EX_TOOLWINDOW);
            }

            User32.SetWindowLong(hwnd, User32.WindowLongFlags.GWL_EXSTYLE, (IntPtr)v);
        }

        public static bool IsWindowOfProcessElevated(IntPtr hwnd)
        {
            if (hwnd == IntPtr.Zero) return false;

            lock (isWindowOfProcessElevatedCache)
            {
                if (isWindowOfProcessElevatedCache.TryGetValue(hwnd, out var v)) return v;

                if (User32.GetWindowThreadProcessId(hwnd, out var pid) > 0 && pid > 0)
                {
                    const uint PROCESS_QUERY_LIMITED_INFORMATION = 0x1000;

                    using (var process = Kernel32.OpenProcess(PROCESS_QUERY_LIMITED_INFORMATION, false, pid))
                    {
                        if (AdvApi32.OpenProcessToken(process, AdvApi32.TokenAccess.TOKEN_QUERY, out var tokenHandle))
                        {
                            using (tokenHandle)
                            {
                                var elevated = tokenHandle.GetInfo<AdvApi32.TOKEN_ELEVATION>(AdvApi32.TOKEN_INFORMATION_CLASS.TokenElevation).TokenIsElevated;
                                isWindowOfProcessElevatedCache[hwnd] = elevated;

                                return elevated;
                            }
                        }
                    }
                }
            }
            return false;
        }

        public static string ProcessCopyDataMessage(IntPtr lParam)
        {
            if (lParam == IntPtr.Zero) return string.Empty;

            try
            {
                var data = Marshal.PtrToStructure<COPYDATASTRUCT>(lParam);
                return data.lpData ?? string.Empty;
            }
            catch (Exception ex)
            {
                HotLyric.Win32.Utils.LogHelper.LogError(ex);
            }
            return string.Empty;
        }

        public static void SendCopyDataMessage(IntPtr hwnd, string message)
        {
            if (hwnd == IntPtr.Zero) return;

            var data = new COPYDATASTRUCT()
            {
                cbData = message.Length + 1,
                lpData = message + "\0"
            };

            var result = User32.SendMessage(hwnd, (uint)User32.WindowMessage.WM_COPYDATA, IntPtr.Zero, ref data);
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct COPYDATASTRUCT
        {
            public IntPtr dwData;    // Any value the sender chooses.  Perhaps its main window handle?
            public int cbData;       // The count of bytes in the message.

            [MarshalAs(UnmanagedType.LPStr)]
            public string lpData;    // The address of the message.
        }

    }
}
