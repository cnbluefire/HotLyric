using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Printing;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Interop;
using System.Windows.Threading;
using Vanara.PInvoke;

namespace HotLyric.Win32.Utils.WindowBackgrounds
{
    internal class WindowBackgroundInputSink : IDisposable
    {
        private InnerWindow innerWindow;
        private bool disposedValue;
        private Window window;
        private IntPtr hwnd;
        private HwndSource? hwndSource;
        private HwndSourceHook? hwndSourceHook;
        private bool isTransparent;
        private DispatcherTimer? updateStateTimer;
        private DispatcherTimer? touchTimer;

        public WindowBackgroundInputSink(Window window)
        {
            this.window = window;
            innerWindow = new InnerWindow(this);

            Initialize();
        }

        private async void Initialize()
        {
            window.Closed += Window_Closed;
            window.IsVisibleChanged += Window_IsVisibleChanged;

            var handle = await WindowHelper.GetWindowHandleAsync(window);
            if (disposedValue) return;

            hwnd = handle;
            if (hwnd != IntPtr.Zero)
            {
                hwndSource = HwndSource.FromHwnd(hwnd);

                if (hwndSource == null)
                {
                    hwnd = IntPtr.Zero;
                }
                else
                {
                    hwndSourceHook = new HwndSourceHook(WndProc);

                    hwndSource.AddHook(hwndSourceHook);

                    if (window.IsVisible)
                    {
                        UpdateInnerWindowPosAndShow();
                    }

                    innerWindow.MouseEnter += InnerWindow_MouseEnter;
                    innerWindow.MouseLeave += InnerWindow_MouseLeave;
                    innerWindow.MouseMove += InnerWindow_MouseMove;

                    innerWindow.FollowWindowHandle = hwnd;

                    updateStateTimer = new DispatcherTimer(DispatcherPriority.Normal)
                    {
                        Interval = TimeSpan.FromMilliseconds(500)
                    };

                    updateStateTimer.Tick += (s, a) => UpdateMouseState();

                    touchTimer = new DispatcherTimer(DispatcherPriority.Background)
                    {
                        Interval = TimeSpan.FromMilliseconds(3000)
                    };

                    touchTimer.Tick += (s, a) =>
                    {
                        if (disposedValue) return;

                        var oldValue = MouseOverWindow;

                        MouseOverWindow = false;

                        touchTimer?.Stop();
                        updateStateTimer?.Stop();

                        if (MouseOverWindow != oldValue)
                        {
                            MouseStateChanged?.Invoke(this, EventArgs.Empty);
                        }
                    };

                    UpdateMouseState();
                }
            }

            if (hwnd == IntPtr.Zero)
            {
                Clear();
            }

        }

        private void InnerWindow_MouseEnter(object? sender, EventArgs e)
        {
            UpdateMouseState();
            if (!updateStateTimer!.IsEnabled)
            {
                updateStateTimer.Start();
            }
        }

        private void InnerWindow_MouseMove(object sender, MouseEventArgs e)
        {
            if (innerWindow == null || (DateTime.UtcNow - innerWindow.lastTouchTime).TotalSeconds > 0.5)
            {
                touchTimer?.Stop();
            }

            UpdateMouseState();
            if (!updateStateTimer!.IsEnabled)
            {
                updateStateTimer.Start();
            }
        }

        private void InnerWindow_MouseLeave(object? sender, EventArgs e)
        {
            UpdateMouseState();
        }

        private void UpdateMouseState()
        {
            var oldValue = MouseOverWindow;

            if (innerWindow.Visible
                && User32.GetCursorPos(out var point))
            {
                MouseOverWindow = innerWindow.IsPointOverWindow(point.X, point.Y);
            }
            else
            {
                MouseOverWindow = false;
            }

            if (!MouseOverWindow)
            {
                updateStateTimer?.Stop();
            }

            if (MouseOverWindow != oldValue)
            {
                MouseStateChanged?.Invoke(this, EventArgs.Empty);
            }

        }


        public bool IsTransparent
        {
            get => isTransparent;
            set
            {
                isTransparent = value;
                UpdateInnerWindowPosAndShow();
            }
        }

        public bool MouseOverWindow { get; private set; }

        public event EventHandler? MouseStateChanged;

        private void StartTouchTimer()
        {
            updateStateTimer?.Stop();
            touchTimer?.Stop();
            touchTimer?.Start();
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (hwnd == this.hwnd && msg == (int)User32.WindowMessage.WM_WINDOWPOSCHANGED)
            {
                UpdateInnerWindowPosAndShow();
            }
            return IntPtr.Zero;
        }

        private void Window_Closed(object? sender, EventArgs e)
        {
            Clear();
        }

        private void Window_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue is true)
            {
                UpdateInnerWindowPosAndShow();
            }
            else
            {
                innerWindow.Hide();
            }
        }

        private void UpdateInnerWindowPosAndShow()
        {
            if (innerWindow.IsDragMoving) return;

            if (!IsTransparent
                && User32.IsWindowVisible(hwnd)
                && !User32.IsIconic(hwnd)
                && User32.GetWindowRect(hwnd, out var rect))
            {
                var innerWindowHwnd = innerWindow.Handle;

                if (!innerWindow.Visible) innerWindow.Show();

                User32.SetWindowPos(
                    innerWindowHwnd,
                    hwnd, rect.Left, rect.Top, rect.Width, rect.Height,
                    User32.SetWindowPosFlags.SWP_SHOWWINDOW
                        | User32.SetWindowPosFlags.SWP_NOACTIVATE);

                UpdateMouseState();
            }
            else
            {
                updateStateTimer?.Stop();
                if (innerWindow.Visible)
                {
                    innerWindow.Hide();
                }
            }
        }

        private void Clear()
        {
            MouseOverWindow = false;

            updateStateTimer?.Stop();
            updateStateTimer = null;

            touchTimer?.Stop();
            touchTimer = null;

            if (hwndSource != null)
            {
                if (hwndSourceHook != null)
                {
                    hwndSource.RemoveHook(hwndSourceHook);
                    hwndSourceHook = null;
                }

                hwndSource = null;
            }

            if (window != null)
            {
                window.Closed -= Window_Closed;
                window.IsVisibleChanged -= Window_IsVisibleChanged;
                window = null!;
            }

            if (innerWindow != null)
            {
                innerWindow.MouseEnter -= InnerWindow_MouseEnter;
                innerWindow.MouseLeave -= InnerWindow_MouseLeave;
                innerWindow.MouseMove -= InnerWindow_MouseMove;

                innerWindow?.Close();
                innerWindow = null!;
            }


        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: 释放托管状态(托管对象)

                    Clear();
                }

                // TODO: 释放未托管的资源(未托管的对象)并重写终结器
                // TODO: 将大型字段设置为 null
                disposedValue = true;
            }
        }

        // // TODO: 仅当“Dispose(bool disposing)”拥有用于释放未托管资源的代码时才替代终结器
        // ~WindowBackgroundInputSink()
        // {
        //     // 不要更改此代码。请将清理代码放入“Dispose(bool disposing)”方法中
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            // 不要更改此代码。请将清理代码放入“Dispose(bool disposing)”方法中
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        private class InnerWindow : System.Windows.Forms.Form
        {
            private WindowBackgroundInputSink inputSink;
            internal DateTime lastTouchTime;

            internal InnerWindow(WindowBackgroundInputSink inputSink)
            {
                this.inputSink = inputSink;
            }

            internal IntPtr FollowWindowHandle { get; set; }
            internal Window? FollowWindow { get; set; }

            internal bool IsDragMoving { get; private set; }

            protected override CreateParams CreateParams
            {
                get
                {
                    var cp = base.CreateParams;

                    cp.Style = (int)(User32.WindowStyles.WS_CLIPCHILDREN);

                    cp.ExStyle = (int)(User32.WindowStylesEx.WS_EX_TOOLWINDOW
                        | User32.WindowStylesEx.WS_EX_NOACTIVATE
                        | User32.WindowStylesEx.WS_EX_NOREDIRECTIONBITMAP);

                    return cp;
                }
            }

            protected override void OnCreateControl()
            {
                base.OnCreateControl();
                User32.RegisterTouchWindow(Handle, 0);
            }

            internal bool IsPointOverWindow(int x, int y)
            {
                const double DefaultMarginPoint = 12d;
                const double CornerRadiusPoint = 8d;

                var dpi = DeviceDpi;

                var margin = (int)((DefaultMarginPoint) * dpi / 96);
                var cornerRadius = (int)((CornerRadiusPoint) * dpi / 96);

                if (User32.GetWindowRect(Handle, out var rect))
                {
                    rect.Left += margin;
                    rect.Top += margin;
                    rect.Width = Math.Max(rect.Width - margin, 0);
                    rect.Height = Math.Max(rect.Height - margin, 0);

                    if (x >= rect.Left && x <= rect.Right && y >= rect.Top && y <= rect.Bottom)
                    {
                        if (x <= cornerRadius)
                        {
                            if (y <= cornerRadius || y >= rect.bottom - cornerRadius) return false;
                        }
                        if (x >= rect.right - cornerRadius)
                        {
                            if (y <= cornerRadius || y >= rect.bottom - cornerRadius) return false;
                        }

                        return true;
                    }
                }

                return false;
            }

            protected override void WndProc(ref Message m)
            {
                const int WM_TOUCH = 0x0240;

                var followWindowHandle = inputSink.hwnd;

                if (followWindowHandle == IntPtr.Zero) return;

                if (m.Msg == WM_TOUCH)
                {
                    lastTouchTime = DateTime.UtcNow;
                    inputSink.StartTouchTimer();
                }
                else if (m.Msg == (int)User32.WindowMessage.WM_NCHITTEST)
                {
                    var x = Macros.GET_X_LPARAM(m.LParam);
                    var y = Macros.GET_Y_LPARAM(m.LParam);

                    if (IsPointOverWindow(x, y))
                    {
                        m.Result = new IntPtr(0x01);
                    }
                    else
                    {
                        m.Result = new IntPtr(0x00);
                    }
                    return;
                }
                else if (m.Msg == (int)User32.WindowMessage.WM_WINDOWPOSCHANGING)
                {
                    var windowPos = Marshal.PtrToStructure<User32.WINDOWPOS>(m.LParam);

                    var flag = User32.SetWindowPosFlags.SWP_NOZORDER;

                    if ((windowPos.flags & flag) == 0 && windowPos.hwndInsertAfter != followWindowHandle)
                    {
                        windowPos.hwndInsertAfter = followWindowHandle;
                        Marshal.StructureToPtr(windowPos, m.LParam, false);
                    }
                }

                base.WndProc(ref m);
            }
        }
    }
}
