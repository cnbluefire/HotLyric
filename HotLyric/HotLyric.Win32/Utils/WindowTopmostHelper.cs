using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Vanara.PInvoke;

namespace HotLyric.Win32.Utils
{
    public class WindowTopmostHelper : IDisposable
    {
        private bool disposedValue;
        private Window window;
        private IntPtr hwnd;
        private DispatcherTimer timer = null!;
        private System.Timers.Timer? trayWndHandleTimer;
        private volatile HashSet<IntPtr> systemWindowList;
        private IntPtr fwHwnd;
        private CancellationTokenSource? cts;
        private bool hideWhenFullScreenAppOpen;

        public WindowTopmostHelper(Window window)
        {
            this.window = window;

            systemWindowList = new HashSet<IntPtr>();

            window.Closed += Window_Closed;

            Init();
        }

        public bool HideWhenFullScreenAppOpen
        {
            get => hideWhenFullScreenAppOpen;
            set
            {
                if (hideWhenFullScreenAppOpen != value)
                {
                    hideWhenFullScreenAppOpen = value;

                    if (hwnd != IntPtr.Zero)
                    {
                        window.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() => Update()));
                    }
                }
            }
        }

        private async void Init()
        {
            hwnd = await WindowHelper.GetWindowHandleAsync(window);

            timer = new DispatcherTimer(DispatcherPriority.Normal, window.Dispatcher)
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            timer.Tick += Timer_Tick;
            timer.Start();

            trayWndHandleTimer = new System.Timers.Timer(10 * 60 * 1000);
            trayWndHandleTimer.Elapsed += TrayWndHandleTimer_Elapsed;
            trayWndHandleTimer.Start();

            ForegroundWindowHelper.ForegroundWindowChanged += ForegroundWindowHelper_ForegroundWindowChanged;
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            Update();
        }

        private void TrayWndHandleTimer_Elapsed(object? sender, ElapsedEventArgs e)
        {
            // 10分钟清空一次
            systemWindowList = new HashSet<IntPtr>();

            window.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() => Update()));
        }


        private void ForegroundWindowHelper_ForegroundWindowChanged(ForegroundWindowHelperEventArgs args)
        {
            fwHwnd = args.Hwnd;

            bool flag = false;

            if (systemWindowList.Contains(fwHwnd))
            {
                flag = true;
            }
            else if (args.IsSystemWindow)
            {
                systemWindowList.Add(fwHwnd);
                flag = true;
            }

            if (flag || GameModeHelper.FullScreen)
            {
                Update();
            }
        }

        private async void Update()
        {
            cts?.Cancel();
            cts = null;

            var isFullScreen = hideWhenFullScreenAppOpen && GameModeHelper.FullScreen;
            var isSystemWindow = systemWindowList.Contains(fwHwnd);

            if (isFullScreen && !isSystemWindow)
            {
                var state = GameModeHelper.State();

                if (state == UserNotificationState.QUNS_APP)
                {
                    cts = new CancellationTokenSource();
                    try
                    {
                        await Task.Delay(200, cts.Token);
                        if (!GameModeHelper.GameMode) return;
                    }
                    catch
                    {
                        return;
                    }
                }

                var sb = new StringBuilder(256);
                User32.GetClassName(fwHwnd, sb, sb.Capacity);
                var className = sb.ToString();
                Debug.WriteLine(className);

                if (timer.Interval.TotalSeconds != 2)
                {
                    timer.Interval = TimeSpan.FromSeconds(2);
                }

                if (window.IsVisible)
                {
                    window.Topmost = false;
                    WindowHelper.SetTopmost(hwnd, false);
                }
            }
            else
            {
                if (timer.Interval.TotalSeconds != 1)
                {
                    timer.Interval = TimeSpan.FromSeconds(1);
                }

                if (window is IHostWindow hostWindow
                    && hostWindow.ChildWindow != null
                    && !HasOpenedPopup(hostWindow.ChildWindow))
                {
                    window.Topmost = true;
                    WindowHelper.SetTopmost(hwnd, true);
                }
                else
                {
                    WindowHelper.SetTopmost(hwnd, false);
                    window.Topmost = false;
                }
            }
        }

        public static bool HasOpenedPopup(Window? window = null, bool ignoreTooltip = true)
        {
            return PresentationSource.CurrentSources.OfType<System.Windows.Interop.HwndSource>()
                .Select(h => h.RootVisual)
                .OfType<FrameworkElement>()
                .Select(f => f.Parent)
                .OfType<System.Windows.Controls.Primitives.Popup>()
                .Where(c => window == null || Window.GetWindow(c) == window)
                .Where(c => !ignoreTooltip || !(c.Child is ToolTip))
                .Any(p => p.IsOpen);
        }

        private void Window_Closed(object? sender, EventArgs e)
        {
            Dispose();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: 释放托管状态(托管对象)

                    timer?.Stop();
                    timer = null!;

                    trayWndHandleTimer?.Stop();
                    trayWndHandleTimer = null!;

                    ForegroundWindowHelper.ForegroundWindowChanged -= ForegroundWindowHelper_ForegroundWindowChanged;

                    window = null!;
                }

                // TODO: 释放未托管的资源(未托管的对象)并重写终结器
                // TODO: 将大型字段设置为 null
                disposedValue = true;
            }
        }

        // // TODO: 仅当“Dispose(bool disposing)”拥有用于释放未托管资源的代码时才替代终结器
        // ~WindowTopmostHelper()
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
    }
}
