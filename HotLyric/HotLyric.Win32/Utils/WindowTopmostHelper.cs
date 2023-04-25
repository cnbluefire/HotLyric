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
using WinUIEx;

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
