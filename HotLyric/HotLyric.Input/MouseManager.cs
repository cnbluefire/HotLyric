using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using Vanara.PInvoke;
using RawInput = Linearstar.Windows.RawInput.Native;

namespace HotLyric.Input
{
    public static class MouseManager
    {
        private static volatile MouseButtonState leftButton;
        private static volatile MouseButtonState middleButton;
        private static volatile MouseButtonState rightButton;
        private static volatile MouseButtonState xButton1;
        private static volatile MouseButtonState xButton2;
        private static volatile int x;
        private static volatile int y;

        private static WorkerThread? workerThread;
        private static RawInputWindow? rawInputWindow;
        private static User32.SafeHHOOK? hhook;
        private static Kernel32.SafeHINSTANCE? hinstance;
        private static object locker = new object();
        private static bool installed = false;
        private static volatile MouseOperation mouseOperation;
        private static User32.HookProc mouseHookProc = new User32.HookProc(MouseHookProc);

        public static MouseButtonState LeftButton => leftButton;

        public static MouseButtonState MiddleButton => middleButton;

        public static MouseButtonState RightButton => rightButton;

        public static MouseButtonState XButton1 => xButton1;

        public static MouseButtonState XButton2 => xButton2;

        public static int X => x;

        public static int Y => y;

        public static bool Installed => installed;

        public static event Events.MouseEventHandler? MouseMoved;

        public static event Events.MouseButtonEventHandler? MouseDown;

        public static event Events.MouseButtonEventHandler? MouseUp;

        public static event Events.MouseWheelEventHandler? MouseWheel;

        public static MouseOperation MouseOperation
        {
            get => mouseOperation;
            set
            {
                if (mouseOperation == value) return;
                mouseOperation = value;
            }
        }

        public static bool Install(bool useMouseHook = true)
        {
            if (!installed)
            {
                lock (locker)
                {
                    if (!installed)
                    {
                        return InstallCore(useMouseHook);
                    }
                }
            }
            return false;
        }

        public static void Uninstall()
        {
            if (installed)
            {
                lock (locker)
                {
                    if (installed)
                    {
                        if (hhook != null)
                        {
                            hhook.Dispose();
                            hhook = null;
                        }

                        if (rawInputWindow != null)
                        {
                            rawInputWindow.Input -= RawInputWindow_Input;
                            rawInputWindow.DestroyHandle();
                            rawInputWindow = null;
                        }

                        workerThread?.Dispose();
                        workerThread = null;
                    }
                }
            }
        }

        private static bool InstallCore(bool useMouseHook)
        {
            workerThread?.Dispose();
            workerThread = null;

            if (User32.GetCursorPos(out var pos))
            {
                x = pos.X;
                y = pos.Y;
            }

            bool flag = true;
            var autoResetEvent = new AutoResetEvent(false);

            workerThread = new WorkerThread();
            workerThread.Start();
            _ = workerThread.RunAsync(() =>
            {
                try
                {
                    User32.SetThreadDpiAwarenessContext(new User32.DPI_AWARENESS_CONTEXT((IntPtr)User32.DPI_AWARENESS.DPI_AWARENESS_PER_MONITOR_AWARE));

                    if (hinstance == null)
                    {
                        hinstance = Kernel32.GetModuleHandle("user32");
                    }
                    if (hinstance == null || hinstance.IsInvalid)
                    {
                        throw new InvalidOperationException(nameof(hinstance));
                    }

                    if (useMouseHook)
                    {
                        hhook = User32.SetWindowsHookEx(User32.HookType.WH_MOUSE_LL, mouseHookProc, hinstance, 0);
                        if (hhook.IsInvalid)
                        {
                            hhook.Dispose();
                            hhook = null;
                        }

                        flag = hhook != null;
                    }
                    else
                    {
                        flag = true;
                    }

                    if (flag)
                    {
                        try
                        {
                            rawInputWindow = new RawInputWindow();

                            Linearstar.Windows.RawInput.RawInputDevice.RegisterDevice(
                                Linearstar.Windows.RawInput.HidUsageAndPage.Mouse,
                                Linearstar.Windows.RawInput.RawInputDeviceFlags.InputSink,
                                rawInputWindow.Handle);

                            rawInputWindow.Input += RawInputWindow_Input;
                        }
                        catch
                        {
                            try
                            {
                                if (rawInputWindow != null)
                                {
                                    rawInputWindow.Input -= RawInputWindow_Input;
                                    rawInputWindow.DestroyHandle();
                                }
                            }
                            catch { }

                            rawInputWindow = null;
                            flag = false;
                        }
                    }
                }
                finally
                {
                    autoResetEvent.Set();
                }
            });

            autoResetEvent.WaitOne();
            autoResetEvent.Dispose();

            if (!flag)
            {
                workerThread.Dispose();
                workerThread = null;
            }

            return flag;
        }

        private static void RawInputWindow_Input(object? sender, RawInputEventArgs e)
        {
            if (e.Data is Linearstar.Windows.RawInput.RawInputMouseData mouseData)
            {
                var m = mouseData.Mouse;

                if ((m.Flags & RawInput.RawMouseFlags.MoveAbsolute) != 0)
                {
                    var pos = MouseOperationHelper.GetCursorPosFromAbsolute(m.LastX, m.LastY, (m.Flags & RawInput.RawMouseFlags.VirtualDesktop) != 0);
                    if (x != pos.X || y != pos.Y)
                    {
                        x = pos.X;
                        y = pos.Y;

                        OnMouseMove();
                    }
                }
                else
                {
                    if (m.LastX != 0 || m.LastY != 0)
                    {
                        if (User32.GetCursorPos(out var pos))
                        {
                            x = pos.X;
                            y = pos.Y;

                            OnMouseMove();
                        }
                    }
                }

                if (m.Buttons != RawInput.RawMouseButtonFlags.None
                    && MouseOperationHelper.ConvertToMouseOperation(m.Buttons, out var operation))
                {
                    MouseOperationHelper.TryRaiseMouseWheelEvent(args =>
                    {
                        OnMouseWheelChanged(ref args);
                    }, operation, m.ButtonData);

                    MouseOperationHelper.TryRaiseMouseButtonEvent((isPressed, args) =>
                    {
                        OnMouseButtonChanged(args.ChangeButton, isPressed, ref args);
                    }, operation);
                }
            }
        }

        private static void OnMouseMove()
        {
            MouseMoved?.Invoke(Events.MouseEventArgs.Create());
        }

        private static void OnMouseWheelChanged(ref Events.MouseWheelEventArgs args)
        {
            MouseWheel?.Invoke(args);
        }

        private static void OnMouseButtonChanged(MouseButton button, bool isPressed, ref Events.MouseButtonEventArgs args)
        {
            switch (button)
            {
                case MouseButton.Left:
                    leftButton = isPressed ? MouseButtonState.Pressed : MouseButtonState.Released;
                    break;

                case MouseButton.Middle:
                    middleButton = isPressed ? MouseButtonState.Pressed : MouseButtonState.Released;
                    break;

                case MouseButton.Right:
                    rightButton = isPressed ? MouseButtonState.Pressed : MouseButtonState.Released;
                    break;

                case MouseButton.XButton1:
                    xButton1 = isPressed ? MouseButtonState.Pressed : MouseButtonState.Released;
                    break;

                case MouseButton.XButton2:
                    xButton2 = isPressed ? MouseButtonState.Pressed : MouseButtonState.Released;
                    break;

                default:
                    break;
            }

            if (isPressed)
            {
                MouseDown?.Invoke(args);
            }
            else
            {
                MouseUp?.Invoke(args);
            }
        }

        private static IntPtr MouseHookProc(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode < 0)
            {
                return User32.CallNextHookEx(hhook, nCode, wParam, lParam);
            }

            bool handled = false;
            IntPtr result;

            try
            {
                handled = ProcessMouseMessageCore(wParam, lParam);
            }
            finally
            {
                result = User32.CallNextHookEx(hhook, nCode, wParam, lParam);
                if (handled)
                {
                    result = (IntPtr)1;
                }
            }

            return result;
        }

        private static bool ProcessMouseMessageCore(IntPtr wParam, IntPtr lParam)
        {
            if (MouseOperationHelper.MessageToOperation(wParam, lParam, out var operation, out var msg, out var lParamData, out var mouseMove))
            {
                if (!mouseMove)
                {
                    return (mouseOperation & operation) != 0;
                }
            }
            return false;
        }
    }
}
