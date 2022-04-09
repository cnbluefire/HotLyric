using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;
using Vanara.PInvoke;

namespace HotLyric.Input
{
    [Flags]
    public enum MouseOperation : uint
    {
        None = 0,

        LeftButtonDown = 1 << 0,
        LeftButtonUp = 1 << 1,
        LeftButtonDoubleClick = 1 << 2,

        LeftButtonClick = LeftButtonDown | LeftButtonUp,

        RightButtonDown = 1 << 3,
        RightButtonUp = 1 << 4,
        RightButtonDoubleClick = 1 << 5,

        RightButtonClick = RightButtonDown | RightButtonUp,

        MiddleButtonDown = 1 << 6,
        MiddleButtonUp = 1 << 7,
        MiddleButtonDoubleClick = 1 << 8,

        MiddleButtonClick = MiddleButtonDown | MiddleButtonUp,

        MouseWheel = 1 << 9,

        XButton1Down = 1 << 10,
        XButton1Up = 1 << 11,
        XButton1DoubleClick = 1 << 12,

        XButton1Click = XButton1Down | XButton1Up,

        XButton2Down = 1 << 10,
        XButton2Up = 1 << 11,
        XButton2DoubleClick = 1 << 12,

        XButton2Click = XButton2Down | XButton2Up,

        MouseHorizontalWheel = 1 << 13
    }

    internal static class MouseOperationHelper
    {
        internal static int GetXButton(IntPtr wParam)
        {
            return SignedHIWORD(wParam);
        }

        internal static bool MessageToOperation(IntPtr wParam, IntPtr lParam, out MouseOperation operation, out int message, out User32.MSLLHOOKSTRUCT lParamData, out bool mouseMove)
        {
            lParamData = default;
            operation = MouseOperation.None;

            message = IntPtrToInt32(wParam);
            mouseMove = (message == (int)User32.WindowMessage.WM_MOUSEMOVE);

            switch ((User32.WindowMessage)message)
            {
                case User32.WindowMessage.WM_LBUTTONDOWN:
                    operation = MouseOperation.LeftButtonDown;
                    break;

                case User32.WindowMessage.WM_LBUTTONUP:
                    operation = MouseOperation.LeftButtonUp;
                    break;

                case User32.WindowMessage.WM_LBUTTONDBLCLK:
                    operation = MouseOperation.LeftButtonDoubleClick;
                    break;

                case User32.WindowMessage.WM_RBUTTONDOWN:
                    operation = MouseOperation.RightButtonDown;
                    break;

                case User32.WindowMessage.WM_RBUTTONUP:
                    operation = MouseOperation.RightButtonUp;
                    break;

                case User32.WindowMessage.WM_RBUTTONDBLCLK:
                    operation = MouseOperation.RightButtonDoubleClick;
                    break;

                case User32.WindowMessage.WM_MBUTTONDOWN:
                    operation = MouseOperation.MiddleButtonDown;
                    break;

                case User32.WindowMessage.WM_MBUTTONUP:
                    operation = MouseOperation.MiddleButtonUp;
                    break;

                case User32.WindowMessage.WM_MBUTTONDBLCLK:
                    operation = MouseOperation.MiddleButtonDoubleClick;
                    break;

                case User32.WindowMessage.WM_XBUTTONDOWN:
                    operation = MouseOperation.XButton1Down;
                    break;

                case User32.WindowMessage.WM_XBUTTONUP:
                    operation = MouseOperation.XButton1Up;
                    break;

                case User32.WindowMessage.WM_XBUTTONDBLCLK:
                    operation = MouseOperation.XButton1DoubleClick;
                    break;

                case User32.WindowMessage.WM_MOUSEWHEEL:
                    operation = MouseOperation.MouseWheel;
                    break;

                case User32.WindowMessage.WM_MOUSEHWHEEL:
                    operation = MouseOperation.MouseHorizontalWheel;
                    break;

                default:
                    operation = MouseOperation.None;
                    break;
            }

            bool flag = false;
            if (operation != MouseOperation.None || mouseMove)
            {
                try
                {
                    flag = true;
                    lParamData = Marshal.PtrToStructure<User32.MSLLHOOKSTRUCT>(lParam);
                }
                catch
                {
                    operation = MouseOperation.None;
                }
            }

            if (flag)
            {
                if (operation == MouseOperation.XButton1Down && lParamData.mouseData == 2)
                {
                    operation = MouseOperation.XButton2Down;
                }

                if (operation == MouseOperation.XButton1Up && lParamData.mouseData == 2)
                {
                    operation = MouseOperation.XButton2Up;
                }

                if (operation == MouseOperation.XButton1DoubleClick && lParamData.mouseData == 2)
                {
                    operation = MouseOperation.XButton2DoubleClick;
                }
            }

            return flag;
        }

        internal static Point GetCursorPosFromAbsolute(int lastX, int lastY, bool virtualDesktop)
        {
            const float absoluteDesktopWidth = 65535f;
            const float absoluteDesktopHeight = 65535f;

            var desktopWidth = User32.GetSystemMetrics(virtualDesktop ? User32.SystemMetric.SM_CXVIRTUALSCREEN : User32.SystemMetric.SM_CXSCREEN);
            var desktopHeight = User32.GetSystemMetrics(virtualDesktop ? User32.SystemMetric.SM_CYVIRTUALSCREEN : User32.SystemMetric.SM_CYSCREEN);

            var absoluteX = (int)(lastX / absoluteDesktopWidth * desktopWidth);
            var absoluteY = (int)(lastY / absoluteDesktopHeight * desktopHeight);

            return new Point(absoluteX, absoluteY);
        }

        internal static bool TryRaiseMouseWheelEvent(Action<Events.MouseWheelEventArgs> raiseAction, MouseOperation operation, int delta)
        {
            if (raiseAction == null) return false;

            var flags = false;
            if ((operation & MouseOperation.MouseWheel) != 0)
            {
                flags = true;
                var args = Events.MouseWheelEventArgs.Create(delta, Events.Orientation.Vertical);
                raiseAction.Invoke(args);
            }
            if ((operation & MouseOperation.MouseHorizontalWheel) != 0)
            {
                flags = true;
                var args = Events.MouseWheelEventArgs.Create(delta, Events.Orientation.Horizontal);
                raiseAction.Invoke(args);
            }
            return flags;
        }

        internal static bool TryRaiseMouseButtonEvent(Action<bool, Events.MouseButtonEventArgs> raiseAction, MouseOperation operation)
        {
            if (raiseAction == null) return false;

            var flag = false;
            while (ConvertToMouseButton(operation, out var button, out var nextOperation))
            {
                flag = true;
                var args = Events.MouseButtonEventArgs.Create(button);
                raiseAction.Invoke(IsPressed(operation), args);
                operation = nextOperation;
            }

            return flag;
        }

        private static bool IsPressed(MouseOperation operation)
        {
            const MouseOperation PressedFlags = MouseOperation.LeftButtonDown | MouseOperation.RightButtonDown | MouseOperation.MiddleButtonDown | MouseOperation.XButton1Down | MouseOperation.XButton2Down;

            return (operation & PressedFlags) > 0;
        }

        private static bool ConvertToMouseButton(MouseOperation operation, out MouseButton button, out MouseOperation nextOperation)
        {
            const MouseOperation LeftFlags = MouseOperation.LeftButtonClick | MouseOperation.LeftButtonDoubleClick;
            const MouseOperation RightFlags = MouseOperation.RightButtonClick | MouseOperation.RightButtonDoubleClick;
            const MouseOperation MiddleFlags = MouseOperation.MiddleButtonClick | MouseOperation.MiddleButtonDoubleClick;
            const MouseOperation X1Flags = MouseOperation.XButton1Click | MouseOperation.XButton1DoubleClick;
            const MouseOperation X2Flags = MouseOperation.XButton2Click | MouseOperation.XButton2DoubleClick;

            button = default;
            nextOperation = MouseOperation.None;

            if ((operation & LeftFlags) > 0)
            {
                button = MouseButton.Left;
                nextOperation = operation & (~LeftFlags);
            }
            else if ((operation & RightFlags) > 0)
            {
                button = MouseButton.Right;
                nextOperation = operation & (~RightFlags);
            }
            else if ((operation & MiddleFlags) > 0)
            {
                button = MouseButton.Middle;
                nextOperation = operation & (~MiddleFlags);
            }
            else if ((operation & X1Flags) > 0)
            {
                button = MouseButton.XButton1;
                nextOperation = operation & (~X1Flags);
            }
            else if ((operation & X2Flags) > 0)
            {
                button = MouseButton.XButton2;
                nextOperation = operation & (~X2Flags);
            }
            else return false;

            return true;
        }

        internal static bool ConvertToMouseOperation(Linearstar.Windows.RawInput.Native.RawMouseButtonFlags rawMouseButtonFlag, out MouseOperation operation)
        {
            operation = MouseOperation.None;

            if ((rawMouseButtonFlag & Linearstar.Windows.RawInput.Native.RawMouseButtonFlags.LeftButtonDown) != 0)
            {
                operation |= MouseOperation.LeftButtonDown;
            }

            if ((rawMouseButtonFlag & Linearstar.Windows.RawInput.Native.RawMouseButtonFlags.LeftButtonUp) != 0)
            {
                operation |= MouseOperation.LeftButtonUp;
            }

            if ((rawMouseButtonFlag & Linearstar.Windows.RawInput.Native.RawMouseButtonFlags.RightButtonDown) != 0)
            {
                operation |= MouseOperation.RightButtonDown;
            }

            if ((rawMouseButtonFlag & Linearstar.Windows.RawInput.Native.RawMouseButtonFlags.RightButtonUp) != 0)
            {
                operation |= MouseOperation.RightButtonUp;
            }

            if ((rawMouseButtonFlag & Linearstar.Windows.RawInput.Native.RawMouseButtonFlags.MiddleButtonDown) != 0)
            {
                operation |= MouseOperation.MiddleButtonDown;
            }

            if ((rawMouseButtonFlag & Linearstar.Windows.RawInput.Native.RawMouseButtonFlags.MiddleButtonUp) != 0)
            {
                operation |= MouseOperation.MiddleButtonUp;
            }

            if ((rawMouseButtonFlag & Linearstar.Windows.RawInput.Native.RawMouseButtonFlags.Button4Down) != 0)
            {
                operation |= MouseOperation.XButton1Down;
            }

            if ((rawMouseButtonFlag & Linearstar.Windows.RawInput.Native.RawMouseButtonFlags.Button4Up) != 0)
            {
                operation |= MouseOperation.XButton1Up;
            }

            if ((rawMouseButtonFlag & Linearstar.Windows.RawInput.Native.RawMouseButtonFlags.Button5Down) != 0)
            {
                operation |= MouseOperation.XButton2Down;
            }

            if ((rawMouseButtonFlag & Linearstar.Windows.RawInput.Native.RawMouseButtonFlags.Button5Up) != 0)
            {
                operation |= MouseOperation.XButton2Up;
            }

            if ((rawMouseButtonFlag & Linearstar.Windows.RawInput.Native.RawMouseButtonFlags.MouseWheel) != 0)
            {
                operation |= MouseOperation.MouseWheel;
            }

            if ((rawMouseButtonFlag & Linearstar.Windows.RawInput.Native.RawMouseButtonFlags.MouseHorizontalWheel) != 0)
            {
                operation |= MouseOperation.MouseHorizontalWheel;
            }

            return operation != MouseOperation.None;
        }

        public static int SignedHIWORD(IntPtr intPtr)
        {
            return SignedHIWORD(IntPtrToInt32(intPtr));
        }

        public static int SignedLOWORD(IntPtr intPtr)
        {
            return SignedLOWORD(IntPtrToInt32(intPtr));
        }

        public static int SignedHIWORD(int n)
        {
            int i = (int)(short)((n >> 16) & 0xffff);

            return i;
        }

        public static int SignedLOWORD(int n)
        {
            int i = (int)(short)(n & 0xFFFF);

            return i;
        }

        public static int IntPtrToInt32(IntPtr intPtr)
        {
            return unchecked((int)intPtr.ToInt64());
        }
    }
}
