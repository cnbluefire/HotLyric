using HotLyric.Win32.Base;
using HotLyric.Win32.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Vanara.PInvoke;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using WinUIEx;
using Windows.Foundation;
using System.Numerics;

namespace HotLyric.Win32.Utils
{
    internal static class WindowDragCommandHelper
    {
        private const int VK_LBUTTON = 0x01;

        private static SemaphoreSlim locker = new SemaphoreSlim(1, 1);
        private static Window? draggingWindow;
        private static Pointer? draggingPointer;
        private static PointerPoint? draggingPointerPoint;
        private static POINT originalPoint;
        private static RECT originalWindowRect;
        private static DragCommand draggingCommand;
        private static DragResizeEdge draggingEdge;
        private static DateTime lastTouchOperationTime;

        private static PointerEventHandler pointerMoveEventHandler = new(OnPointerMoved);
        private static PointerEventHandler pointerReleasedEventHandler = new(OnPointerReleased);
        private static PointerEventHandler pointerCaptureLostEventHandler = new(OnPointerCaptureLost);

        public static async Task<bool> DragMoveAsync(this Window window)
        {
            return await DragMoveAsync(window, null).ConfigureAwait(false);
        }

        public static async Task<bool> DragMoveAsync(this Window window, PointerRoutedEventArgs? pointerEventArgs)
        {
            return await DoDragCommandAsync(window, pointerEventArgs, DragCommand.Move, 0).ConfigureAwait(false);
        }

        public static async Task<bool> DragResizeAsync(this Window window, DragResizeEdge edge)
        {
            return await DragResizeAsync(window, null, edge).ConfigureAwait(false);
        }

        public static async Task<bool> DragResizeAsync(this Window window, PointerRoutedEventArgs? pointerEventArgs, DragResizeEdge edge)
        {
            return await DoDragCommandAsync(window, pointerEventArgs, DragCommand.Resize, edge).ConfigureAwait(false);
        }

        private static async Task<bool> DoDragCommandAsync(this Window window, PointerRoutedEventArgs? pointerEventArgs, DragCommand command, DragResizeEdge edge)
        {
            if (window == null) return false;

            if (pointerEventArgs != null)
            {
                var pointer = pointerEventArgs.Pointer;
                var point = pointerEventArgs.GetCurrentPoint(window.Content);

                if (point != null && point.Properties.IsLeftButtonPressed && point.Properties.IsPrimary)
                {
                    if (command == DragCommand.Move && pointer.PointerDeviceType == PointerDeviceType.Mouse && false)
                    {
                        return await DoMouseDragCommandAsync(window, command, edge).ConfigureAwait(false);
                    }
                    else
                    {
                        return await DoPointerDragCommandAsync(window, pointer, point, command, edge).ConfigureAwait(false);
                    }
                }
            }

            var state = User32.GetKeyState(VK_LBUTTON);
            var pressed = (state & 0x8000) != 0;

            if (pressed)
            {
                return await DoMouseDragCommandAsync(window, command, edge).ConfigureAwait(false);
            }

            return false;
        }

        private static async Task<bool> DoMouseDragCommandAsync(Window window, DragCommand command, DragResizeEdge edge)
        {
            await Task.Delay(1);

            IntPtr res = IntPtr.Zero;

            if (User32.ReleaseCapture())
            {
                var cmd = 0;

                if (command == DragCommand.Move)
                {
                    cmd = (int)User32.SysCommand.SC_MOVE + 0x02;
                }
                else
                {
                    cmd = (int)User32.SysCommand.SC_SIZE + (int)edge;
                }

                res = User32.SendMessage(window.GetWindowHandle(), (uint)User32.WindowMessage.WM_SYSCOMMAND, new IntPtr(cmd), IntPtr.Zero);
            }

            User32.SendMessage(window.GetWindowHandle(), (int)User32.WindowMessage.WM_LBUTTONUP, IntPtr.Zero, IntPtr.Zero);

            return res != IntPtr.Zero;
        }

        private static async Task<bool> DoPointerDragCommandAsync(Window window, Pointer pointer, PointerPoint point, DragCommand command, DragResizeEdge edge)
        {
            UIElement? content = null;

            try
            {
                locker.Wait();

                if (draggingWindow != null) return false;

                if (User32.GetWindowRect(window.GetWindowHandle(), out originalWindowRect))
                {
                    if (pointer.PointerDeviceType == PointerDeviceType.Mouse)
                    {
                        User32.GetCursorPos(out originalPoint);
                    }
                    else
                    {
                        originalPoint = GetCurrentPointerPosition(window, point);
                    }

                    content = window.Content;

                    content.RemoveHandler(UIElement.PointerMovedEvent, pointerMoveEventHandler);
                    content.RemoveHandler(UIElement.PointerReleasedEvent, pointerReleasedEventHandler);
                    content.RemoveHandler(UIElement.PointerCaptureLostEvent, pointerCaptureLostEventHandler);

                    if (content.CapturePointer(pointer))
                    {
                        draggingWindow = window;
                        draggingPointer = pointer;
                        draggingPointerPoint = point;
                        draggingCommand = command;
                        draggingEdge = edge;
                    }
                }

                if (draggingWindow == null)
                {
                    return false;
                }
            }
            finally
            {
                locker.Release();
            }

            content?.AddHandler(UIElement.PointerMovedEvent, pointerMoveEventHandler, true);
            content?.AddHandler(UIElement.PointerReleasedEvent, pointerReleasedEventHandler, true);
            content?.AddHandler(UIElement.PointerCaptureLostEvent, pointerCaptureLostEventHandler, true);

            await Task.Yield();

            MSG msg;

            while (User32.GetMessage(out msg) > 0 && draggingWindow != null)
            {
                User32.TranslateMessage(in msg);
                User32.DispatchMessage(in msg);
            }

            return true;
        }


        private static void OnPointerMoved(object sender, global::Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            if (e.Pointer.PointerId != draggingPointerPoint?.PointerId) return;

            var window = draggingWindow;
            if (window == null) return;

            POINT curPoint;

            if (e.Pointer.PointerDeviceType == PointerDeviceType.Mouse)
            {
                User32.GetCursorPos(out curPoint);
                lastTouchOperationTime = default;
            }
            else
            {
                curPoint = GetCurrentPointerPosition(window, e);

                var now = DateTime.Now;
                if ((now - lastTouchOperationTime).TotalMilliseconds < 25) return;

                lastTouchOperationTime = now;
            }

            var offsetX = curPoint.X - originalPoint.X;
            var offsetY = curPoint.Y - originalPoint.Y;

            int x = 0, y = 0, cx = 0, cy = 0;

            if (draggingCommand == DragCommand.Move)
            {
                x = originalWindowRect.X + offsetX;
                y = originalWindowRect.Y + offsetY;

                window.GetAppWindow().Move(new Windows.Graphics.PointInt32(x, y));

            }
            else
            {
                var a1 = GetWindowMaximumSize(window);
                var a2 = GetWindowMinimumSize(window);

                (x, y, cx, cy, var flag2) = GetNewWindowPosition(window, draggingEdge, originalWindowRect, offsetX, offsetY);

                window.GetAppWindow().MoveAndResize(new Windows.Graphics.RectInt32(x, y, cx, cy));
            }
        }

        private static void OnPointerCaptureLost(object sender, global::Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            Stop();
        }

        private static void OnPointerReleased(object sender, global::Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            var window = draggingWindow;
            var point = draggingPointer;

            if (window != null && point != null)
            {
                var content = window?.Content;
                content?.ReleasePointerCapture(point);
            }

            Stop();
        }

        private static void Stop()
        {
            lastTouchOperationTime = default;

            var window = draggingWindow;

            if (window == null) return;

            try
            {
                locker.Wait();

                if (draggingWindow != null)
                {
                    var content = draggingWindow.Content;

                    content.RemoveHandler(UIElement.PointerMovedEvent, pointerMoveEventHandler);
                    content.RemoveHandler(UIElement.PointerReleasedEvent, pointerReleasedEventHandler);
                    content.RemoveHandler(UIElement.PointerCaptureLostEvent, pointerCaptureLostEventHandler);

                    User32.PostMessage(draggingWindow.GetWindowHandle(), (uint)(User32.WindowMessage.WM_USER + 233));
                }

                draggingWindow = null;
                draggingPointer = null;
                draggingPointerPoint = null;
                draggingCommand = default;
                draggingEdge = default;
            }
            finally
            {
                locker.Release();
            }
        }

        private static (int x, int y, int cx, int cy, User32.SetWindowPosFlags flag) GetNewWindowPosition(
            Window window, DragResizeEdge edge, RECT windowRect, int offsetX, int offsetY)
        {
            var rect = originalWindowRect;

            var minSize = GetWindowMinimumSize(window);
            var maxSize = GetWindowMaximumSize(window);
            if (maxSize.IsEmpty)
            {
                maxSize.Width = User32.GetSystemMetrics(User32.SystemMetric.SM_CXMAXTRACK);
                maxSize.Height = User32.GetSystemMetrics(User32.SystemMetric.SM_CYMAXTRACK);
            }

            if (maxSize.Width < minSize.Width || maxSize.Height < minSize.Height)
            {
                return (0, 0, 0, 0, User32.SetWindowPosFlags.SWP_NOMOVE | User32.SetWindowPosFlags.SWP_NOSIZE);
            }

            var isLeft = edge == DragResizeEdge.Left
                || edge == DragResizeEdge.TopLeft
                || edge == DragResizeEdge.BottomLeft;

            var isRight = edge == DragResizeEdge.Right
                || edge == DragResizeEdge.TopRight
                || edge == DragResizeEdge.BottomRight;

            var isTop = edge == DragResizeEdge.Top
                || edge == DragResizeEdge.TopLeft
                || edge == DragResizeEdge.TopRight;

            var isBottom = edge == DragResizeEdge.Bottom
                || edge == DragResizeEdge.BottomLeft
                || edge == DragResizeEdge.BottomRight;

            if (isLeft)
            {
                rect.left += offsetX;

                if (rect.Width > maxSize.Width) rect.left += (rect.Width - maxSize.Width);
                if (rect.Width < minSize.Width) rect.left += (rect.Width - minSize.Width);
            }

            if (isRight)
            {
                rect.right += offsetX;

                if (rect.Width > maxSize.Width) rect.right -= (rect.Width - maxSize.Width);
                if (rect.Width < minSize.Width) rect.right -= (rect.Width - minSize.Width);
            }

            if (isTop)
            {
                rect.top += offsetY;

                if (rect.Height > maxSize.Height) rect.top += (rect.Height - maxSize.Height);
                if (rect.Height < minSize.Height) rect.top += (rect.Height - minSize.Height);
            }

            if (isBottom)
            {
                rect.bottom += offsetY;

                if (rect.Height > maxSize.Height) rect.bottom -= (rect.Height - maxSize.Height);
                if (rect.Height < minSize.Height) rect.bottom -= (rect.Height - minSize.Height);
            }

            return (rect.left, rect.top, rect.Width, rect.Height, 0);
        }

        private static SIZE GetWindowMaximumSize(Window window)
        {
            var manager = WindowManager.Get(window);
            if (manager != null)
            {
                var dpi = window.GetDpiForWindow();
                var maxWidth = manager.MaxWidth * dpi / 96;
                var maxHeight = manager.MaxHeight * dpi / 96;

                return new SIZE((int)maxWidth, (int)maxHeight);
            }
            return new SIZE(0, 0);
        }

        private static SIZE GetWindowMinimumSize(Window window)
        {
            var manager = WindowManager.Get(window);
            if (manager != null)
            {
                var dpi = window.GetDpiForWindow();
                var maxWidth = manager.MinWidth * dpi / 96;
                var maxHeight = manager.MinHeight * dpi / 96;

                return new SIZE((int)maxWidth, (int)maxHeight);
            }
            return new SIZE(0, 0);
        }

        private enum DragCommand
        {
            Move,
            Resize
        }

        private static POINT GetCurrentPointerPosition(Window window, PointerPoint pointerPoint)
        {
            var transform = new WindowPointerPointTransform(window);
            var p = pointerPoint.GetTransformedPoint(transform);
            return new POINT((int)p.Position.X, (int)p.Position.Y);
        }

        private static POINT GetCurrentPointerPosition(Window window, PointerRoutedEventArgs args)
        {
            var transform = new WindowPointerPointTransform(window);
            var p = args.GetCurrentPoint(window.Content).GetTransformedPoint(transform);
            return new POINT((int)p.Position.X, (int)p.Position.Y);
        }

        private class WindowPointerPointTransform : IPointerPointTransform
        {
            private readonly Window window;

            public WindowPointerPointTransform(Window window)
            {
                this.window = window;
            }

            public IPointerPointTransform Inverse => throw new NotImplementedException();

            public bool TryTransform(Point inPoint, out Point outPoint)
            {
                var matrix = GetMatrix();
                outPoint = matrix.Transform(inPoint);
                return true;
            }

            public bool TryTransformBounds(Rect inRect, out Rect outRect)
            {
                var matrix = GetMatrix();
                var point1 = new Point(inRect.Left, inRect.Top);
                var point2 = new Point(inRect.Right, inRect.Bottom);

                var p1 = matrix.Transform(point1);
                var p2 = matrix.Transform(point2);

                outRect = new Rect(Math.Min(p1.X, p2.X), Math.Min(p1.Y, p2.Y), Math.Abs(p1.X - p2.X), Math.Abs(p1.Y - p2.Y));
                return true;
            }

            private Microsoft.UI.Xaml.Media.Matrix GetMatrix()
            {
                var hwnd = window.GetWindowHandle();
                if (User32.GetWindowRect(hwnd, out var wndRect) && User32.GetClientRect(hwnd, out var rect))
                {
                    var dpi = window.GetDpiForWindow();

                    var matrix = Matrix3x2.CreateScale(dpi / 96f)
                        * Matrix3x2.CreateTranslation(wndRect.X + rect.X, wndRect.Y + rect.Y);

                    return new Microsoft.UI.Xaml.Media.Matrix(
                        matrix.M11, matrix.M12, matrix.M21, matrix.M22, matrix.M31, matrix.M32);
                }

                return Microsoft.UI.Xaml.Media.Matrix.Identity;
            }
        }
    }
}
