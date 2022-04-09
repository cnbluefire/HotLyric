using System;
using System.Collections.Generic;
using System.Text;
using Vanara.PInvoke;

namespace HotLyric.Win32.Utils
{
    public static class ForegroundWindowHelper
    {
        private static bool initialized;
        private static User32.HWINEVENTHOOK hook;
        private static User32.WinEventProc? eventProc;

        public static bool Unsupported { get; private set; } = false;

        public static void Initialize()
        {
            if (initialized) return;
            initialized = true;

            eventProc = new User32.WinEventProc(EventProc);

            try
            {
                hook = User32.SetWinEventHook(
                    User32.EventConstants.EVENT_SYSTEM_FOREGROUND,
                    User32.EventConstants.EVENT_SYSTEM_FOREGROUND,
                    HINSTANCE.NULL,
                    eventProc,
                    0,
                    0,
                    User32.WINEVENT.WINEVENT_OUTOFCONTEXT | User32.WINEVENT.WINEVENT_SKIPOWNPROCESS);
            }
            catch { }

            if (hook.IsNull)
            {
                Unsupported = true;
            }
        }

        public static void Uninitialize()
        {
            initialized = false;

            try
            {
                User32.UnhookWinEvent(hook);
                hook = default;
            }
            catch { }
        }

        private static void EventProc(User32.HWINEVENTHOOK hWinEventHook, uint winEvent, HWND hwnd, int idObject, int idChild, uint idEventThread, uint dwmsEventTime)
        {
            try
            {
                ForegroundWindowChanged?.Invoke(new ForegroundWindowHelperEventArgs(hwnd.DangerousGetHandle()));
            }
            catch { }
        }

        public static event ForegroundWindowHelperEventHandler? ForegroundWindowChanged;
    }

    public delegate void ForegroundWindowHelperEventHandler(ForegroundWindowHelperEventArgs args);

    public struct ForegroundWindowHelperEventArgs
    {
        private bool windowTitleFlag;
        private bool windowClassFlag;
        private string? windowTitle;
        private string? windowClassName;

        internal ForegroundWindowHelperEventArgs(IntPtr hwnd)
        {
            windowTitleFlag = false;
            windowClassFlag = false;
            windowTitle = null;
            windowClassName = null;

            Hwnd = hwnd;
        }

        public IntPtr Hwnd { get; }

        public string WindowTitle
        {
            get
            {
                if (Hwnd == IntPtr.Zero) return string.Empty;

                if (!windowTitleFlag)
                {
                    var sb = new StringBuilder(256);
                    User32.GetWindowText(Hwnd, sb, sb.Capacity);
                    windowTitle = sb.ToString();
                    windowTitleFlag = true;
                }

                return windowTitle!;
            }
        }

        public string WindowClassName
        {
            get
            {
                if (Hwnd == IntPtr.Zero) return string.Empty;

                if (!windowClassFlag)
                {
                    var sb = new StringBuilder(256);
                    User32.GetClassName(Hwnd, sb, sb.Capacity);
                    windowClassName = sb.ToString();
                    windowClassFlag = true;
                }

                return windowClassName!;
            }
        }

        public bool IsSystemWindow =>
            string.Equals(WindowClassName, "Shell_TrayWnd", StringComparison.OrdinalIgnoreCase)
            || string.Equals(WindowClassName, "Windows.UI.Core.CoreWindow", StringComparison.OrdinalIgnoreCase)
            || WindowClassName?.StartsWith("HwndWrapper[DefaultDomain;;", StringComparison.OrdinalIgnoreCase) == true;

        public bool IsWindowOfProcessElevated
        {
            get
            {
                try
                {
                    return WindowHelper.IsWindowOfProcessElevated(Hwnd);
                }
                catch { }
                return false;
            }
        }

    }
}
