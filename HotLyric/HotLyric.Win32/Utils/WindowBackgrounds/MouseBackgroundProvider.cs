using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using Vanara.PInvoke;

namespace HotLyric.Win32.Utils.WindowBackgrounds
{
    internal class MouseBackgroundProvider : WindowBackgroundProvider
    {
        private readonly Window window;
        private bool isVisible;
        private DispatcherTimer timer;
        private IntPtr hwnd;
        private Rect bounds = Rect.Empty;
        private object locker = new object();
        private MouseManagerFactory? mouseManagerFactory;
        private bool updating;
        private bool foregroundWindowElevated;

        public MouseBackgroundProvider(Window window) : base(window)
        {
            this.window = window;
            window.LocationChanged += Window_LocationChanged;
            window.SizeChanged += Window_SizeChanged;
            window.IsVisibleChanged += Window_IsVisibleChanged;
            window.Closed += Window_Closed;

            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(0.5);
            timer.Tick += Timer_Tick;

            Input.MouseManager.MouseMoved += MouseManager_MouseMoved;

            isVisible = window.IsVisible;
            UpdateWindowBounds();
            mouseManagerFactory = new MouseManagerFactory();

            ForegroundWindowHelper.ForegroundWindowChanged += ForegroundWindowHelper_ForegroundWindowChanged;

        }

        private void ForegroundWindowHelper_ForegroundWindowChanged(ForegroundWindowHelperEventArgs args)
        {
            if (foregroundWindowElevated != args.IsWindowOfProcessElevated)
            {
                foregroundWindowElevated = args.IsWindowOfProcessElevated;
                DispatcherHelper.UIDispatcher?.BeginInvoke(new Action(() =>
                {
                    UpdateHitTestVisible();
                }), DispatcherPriority.Normal);
            }
        }

        private void Window_Closed(object? sender, EventArgs e)
        {
            isVisible = false;
            UpdateWindowBounds();
        }

        private void Window_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            isVisible = e.NewValue is true;

            Input.MouseManager.MouseMoved -= MouseManager_MouseMoved;

            if (isVisible)
            {
                Input.MouseManager.MouseMoved += MouseManager_MouseMoved;
            }

            UpdateWindowBounds();
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateWindowBounds();
        }

        private void Window_LocationChanged(object? sender, EventArgs e)
        {
            UpdateWindowBounds();
        }

        private void UpdateWindowBounds()
        {
            if (!isVisible) return;

            updating = true;

            timer.Stop();
            timer.Start();
        }

        private async void Timer_Tick(object? sender, EventArgs e)
        {
            updating = false;

            timer.Stop();
            if (hwnd == IntPtr.Zero)
            {
                hwnd = await WindowHelper.GetWindowHandleAsync(window);
            }

            if (!window.IsVisible) return;

            lock (locker)
            {
                if (User32.GetWindowRect(hwnd, out var rect) && rect.Width > 0 && rect.Height > 0)
                {
                    bounds = new Rect(rect.left, rect.top, rect.Width, rect.Height);
                }
                else
                {
                    bounds = Rect.Empty;
                }

                UpdateHitTestVisible();
            }
        }

        private void MouseManager_MouseMoved(Input.Events.MouseEventArgs e)
        {
            if (!isVisible) return;
            if (updating) return;

            window.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() =>
            {
                lock (locker)
                {
                    UpdateHitTestVisible();
                }
            }));
        }

        private void UpdateHitTestVisible()
        {
            bool value = false;

            if (isVisible)
            {
                if (foregroundWindowElevated)
                {
                    value = true;
                }
                else if (!bounds.IsEmpty)
                {
                    if (WindowTopmostHelper.HasOpenedPopup((window as IHostWindow)?.ChildWindow ?? window))
                    {
                        value = true;
                    }
                    else
                    {
                        if (hwnd != IntPtr.Zero
                            && bounds.Contains(Input.MouseManager.X, Input.MouseManager.Y))
                        {
                            value = true;
                        }
                    }
                }
            }

            IsHitTestVisible = value;
        }

        protected override void DisposeCore()
        {
            ForegroundWindowHelper.ForegroundWindowChanged -= ForegroundWindowHelper_ForegroundWindowChanged;

            mouseManagerFactory?.Dispose();
            mouseManagerFactory = null;

            Input.MouseManager.MouseMoved -= MouseManager_MouseMoved;

            timer.Tick -= Timer_Tick;
            timer.Stop();

            window.LocationChanged -= Window_LocationChanged;
            window.SizeChanged -= Window_SizeChanged;
            window.IsVisibleChanged -= Window_IsVisibleChanged;
            window.Closed -= Window_Closed;
        }
    }
}
