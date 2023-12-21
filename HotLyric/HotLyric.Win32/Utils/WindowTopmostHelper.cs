using BlueFire.Toolkit.WinUI3.Extensions;
using BlueFire.Toolkit.WinUI3;
using Microsoft.UI;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using Vanara.PInvoke;

namespace HotLyric.Win32.Utils
{
    public class WindowTopmostHelper : IDisposable
    {
        private bool disposedValue;
        private Window window;
        private IntPtr hwnd;
        private DispatcherTimer? timer;
        private System.Timers.Timer? trayWndHandleTimer;
        private DispatcherQueue? dispatcherQueue;
        private volatile HashSet<IntPtr> systemWindowList;
        private IntPtr fwHwnd;
        private bool hideWhenFullScreenAppOpen;
        private bool enabled = true;
        private User32.MONITORINFO? monitorInfo;
        private System.Drawing.Rectangle? windowBounds;
        private Microsoft.Windows.System.Power.EffectivePowerMode powerMode;

        public WindowTopmostHelper(Window window)
        {
            this.window = window;

            systemWindowList = new HashSet<IntPtr>();

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

                    if (hwnd != IntPtr.Zero && Enabled)
                    {
                        Update();
                    }
                }
            }
        }

        public bool Enabled
        {
            get => enabled;
            set
            {
                if (enabled != value)
                {
                    enabled = value;
                    Update();
                }
            }
        }

        public Microsoft.Windows.System.Power.EffectivePowerMode PowerMode
        {
            get => powerMode;
            set
            {
                if (powerMode != value)
                {
                    powerMode = value;
                    Update();
                }
            }
        }

        private void Init()
        {
            hwnd = window.GetWindowHandle();

            dispatcherQueue = DispatcherQueue.GetForCurrentThread();

            timer = new DispatcherTimer()
            {
                Interval = TimeSpan.FromSeconds(1)
            };

            timer.Tick += Timer_Tick;

            trayWndHandleTimer = new System.Timers.Timer(10 * 60 * 1000);
            trayWndHandleTimer.Elapsed += TrayWndHandleTimer_Elapsed;
            trayWndHandleTimer.Start();

            ForegroundWindowHelper.ForegroundWindowChanged += ForegroundWindowHelper_ForegroundWindowChanged;

            var manager = WindowManager.Get(window);

            if (manager != null)
            {
                manager.WindowMessageReceived += Manager_WindowMessageReceived;
            }
        }

        unsafe private void Manager_WindowMessageReceived(object? sender, WindowMessageReceivedEventArgs e)
        {
            var msg = (User32.WindowMessage)e.MessageId;

            if (msg == User32.WindowMessage.WM_MOVE
                || msg == User32.WindowMessage.WM_SIZE)
            {
                windowBounds = null;
                Update();
            }
            else if (msg == User32.WindowMessage.WM_DISPLAYCHANGE
                || msg == User32.WindowMessage.WM_DPICHANGED)
            {
                windowBounds = null;
                monitorInfo = null;
                Update();
            }
            else if (msg == User32.WindowMessage.WM_WINDOWPOSCHANGED)
            {
                var pWindowPos = (User32.WINDOWPOS*)e.LParam;
                if ((pWindowPos->flags & (User32.SetWindowPosFlags.SWP_NOMOVE | User32.SetWindowPosFlags.SWP_NOSIZE)) == 0)
                {
                    windowBounds = null;
                    Update();
                }
            }
        }

        private void Timer_Tick(object? sender, object e)
        {
            UpdateStateCore();
        }

        private void TrayWndHandleTimer_Elapsed(object? sender, ElapsedEventArgs e)
        {
            // 10分钟清空一次
            systemWindowList = new HashSet<IntPtr>();

            Update();
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

        private void UpdateStateCore()
        {
            var isFullScreen = hideWhenFullScreenAppOpen && GameModeHelper.FullScreen;
            var isSystemWindow = systemWindowList.Contains(fwHwnd);

            if (!isSystemWindow)
            {
                System.Drawing.Rectangle windowBounds = default;

                if (this.windowBounds.HasValue)
                {
                    windowBounds = this.windowBounds.Value;
                }
                else
                {
                    User32.GetWindowRect(hwnd, out var _windowRect);
                    windowBounds = (System.Drawing.Rectangle)_windowRect;
                    this.windowBounds = windowBounds;
                }
                var windowCenterPoint = new System.Drawing.Point(windowBounds.X + windowBounds.Width / 2, windowBounds.Y + windowBounds.Height / 2);

                var monitorInfo = User32.MONITORINFO.Default;
                if (this.monitorInfo.HasValue)
                {
                    monitorInfo = this.monitorInfo.Value;
                    var tmpRect = (System.Drawing.Rectangle)monitorInfo.rcMonitor;
                    if (!tmpRect.Contains(windowCenterPoint))
                    {
                        this.monitorInfo = null;
                    }
                }

                if (!this.monitorInfo.HasValue)
                {
                    var monitor = User32.MonitorFromPoint(windowCenterPoint, User32.MonitorFlags.MONITOR_DEFAULTTONULL);
                    if (!monitor.IsNull)
                    {
                        User32.GetMonitorInfo(monitor, ref monitorInfo);
                        this.monitorInfo = monitorInfo;
                    }
                }

                if (this.monitorInfo.HasValue)
                {
                    var workArea = (System.Drawing.Rectangle)monitorInfo.rcWork;
                    var windowInMonitorRect = windowBounds;

                    windowInMonitorRect.Intersect((System.Drawing.Rectangle)monitorInfo.rcMonitor);

                    if (workArea.Contains(windowInMonitorRect))
                    {
                        timer?.Stop();
                    }
                }
            }

            if (PowerMode == Microsoft.Windows.System.Power.EffectivePowerMode.BetterBattery
                || PowerMode == Microsoft.Windows.System.Power.EffectivePowerMode.BatterySaver)
            {
                timer?.Stop();
            }

            if (isFullScreen && !isSystemWindow)
            {
                if (!GameModeHelper.GameMode) return;

                if (window.Visible)
                {
                    if (window.AppWindow.Presenter is OverlappedPresenter presenter)
                    {
                        presenter.IsAlwaysOnTop = false;
                    }
                }
            }
            else
            {
                if (window.AppWindow.Presenter is OverlappedPresenter presenter)
                {
                    presenter.IsAlwaysOnTop = false;
                    presenter.IsAlwaysOnTop = true;
                }
            }
        }

        private void Update()
        {
            if (dispatcherQueue == null || timer == null) return;

            if (dispatcherQueue.HasThreadAccess)
            {
                timer.Stop();
                timer.Start();
            }
            else
            {
                dispatcherQueue.TryEnqueue(() =>
                {
                    timer.Stop();
                    timer.Start();
                });
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: 释放托管状态(托管对象)

                    var manager = WindowManager.Get(window);
                    if (manager != null)
                    {
                        manager.WindowMessageReceived -= Manager_WindowMessageReceived;
                    }

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
