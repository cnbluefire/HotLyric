using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Vanara.PInvoke;
using WinDispatcherQueueController = Windows.System.DispatcherQueueController;

namespace HotLyric.Win32.Utils
{
    internal class HotKeyListener : IDisposable
    {
        private bool disposedValue;

        private static int id;
        private Thread backgroundThread;
        private WinDispatcherQueueController? dispatcherQueueController;
        private EventWaitHandle? initializeWaitHandle;
        private User32.SafeHWND? messageWindowHandle;
        private User32.WindowProc? wndProcHandler;
        private Dictionary<(User32.HotKeyModifiers modifiers, User32.VK key), int> hotkeyEventHandlers
            = new Dictionary<(User32.HotKeyModifiers modifiers, User32.VK key), int>();

        public HotKeyListener()
        {
            backgroundThread = new Thread(ThreadMain);
            backgroundThread.IsBackground = true;
            backgroundThread.SetApartmentState(ApartmentState.STA);

            initializeWaitHandle = new EventWaitHandle(false, EventResetMode.ManualReset);
            backgroundThread.Start();

            initializeWaitHandle.WaitOne();
            initializeWaitHandle.Dispose();
            initializeWaitHandle = null;
        }

        private void ThreadMain()
        {
            dispatcherQueueController = NativeMethods.CreateCoreDispatcherForCurrentThread();

            try
            {
                CreateMessageWindow();
            }
            catch { }

            initializeWaitHandle!.Set();

            while (User32.GetMessage(out var msg) > 0)
            {
                User32.TranslateMessage(in msg);
                User32.DispatchMessage(in msg);
            }
        }

        private void CreateMessageWindow()
        {
            const nint HWND_MESSAGE = -3;

            var windowName = $"HotKeyWnd";
            var className = $"{windowName}_{Guid.NewGuid()}";

            wndProcHandler = new User32.WindowProc(WndProc);

            var wndClassEx = new User32.WNDCLASSEX()
            {
                cbSize = (uint)Marshal.SizeOf<User32.WNDCLASSEX>(),
                lpfnWndProc = wndProcHandler,
                lpszClassName = className
            };

            var ret = User32.RegisterClassEx(in wndClassEx);

            if (ret != 0)
            {
                messageWindowHandle = User32.CreateWindow(className, windowName, 0, 0, 0, 0, 0, HWND_MESSAGE, 0, Kernel32.GetModuleHandle(), 0);
            }
        }

        private nint WndProc(HWND hwnd, uint uMsg, nint wParam, nint lParam)
        {
            if (uMsg == (uint)User32.WindowMessage.WM_HOTKEY)
            {
                if (wParam != -2 && wParam != -1)
                {
                    lock (hotkeyEventHandlers)
                    {
                        foreach (var ((modifier, key), id) in hotkeyEventHandlers)
                        {
                            if (id == wParam)
                            {
                                dispatcherQueueController?.DispatcherQueue.TryEnqueue(() =>
                                {
                                    try
                                    {
                                        HotKeyInvoked?.Invoke(this, new HotKeyInvokedEventArgs(modifier, key));
                                    }
                                    catch { }
                                });
                            }
                        }
                    }
                }
                return 0;
            }

            return User32.DefWindowProc(hwnd, uMsg, wParam, lParam);
        }

        public async Task<bool> RegisterKey(User32.HotKeyModifiers modifiers, User32.VK key)
        {
            if (modifiers == 0 && key == 0) return false;

            var tcs = new TaskCompletionSource<bool>();

            dispatcherQueueController!.DispatcherQueue.TryEnqueue(() =>
            {
                try
                {
                    int hotKeyId;

                    lock (hotkeyEventHandlers)
                    {
                        if (hotkeyEventHandlers.ContainsKey((modifiers, key)))
                        {
                            tcs.TrySetResult(true);
                            return;
                        }

                        hotKeyId = id;
                        id++;
                    }

                    var res = User32.RegisterHotKey(messageWindowHandle, hotKeyId, modifiers, (uint)key);
                    if (res)
                    {
                        hotkeyEventHandlers[(modifiers, key)] = hotKeyId;
                    }

                    tcs.TrySetResult(res);
                }
                finally
                {
                    tcs.TrySetResult(false);
                }
            });

            return await tcs.Task.ConfigureAwait(false);
        }

        public async Task UnregisterKey(User32.HotKeyModifiers modifiers, User32.VK key)
        {
            if (modifiers == 0 && key == 0) return;

            var tcs = new TaskCompletionSource();

            dispatcherQueueController!.DispatcherQueue.TryEnqueue(() =>
            {
                try
                {
                    lock (hotkeyEventHandlers)
                    {
                        if (hotkeyEventHandlers.Remove((modifiers, key), out var hotKeyId))
                        {
                            User32.UnregisterHotKey(messageWindowHandle, hotKeyId);
                        }
                    }
                }
                finally
                {
                    tcs.TrySetResult();
                }
            });

            await tcs.Task.ConfigureAwait(false);
        }

        public async Task UnregisterAllKeys()
        {
            lock (hotkeyEventHandlers)
            {
                if (hotkeyEventHandlers.Count == 0) return;
            }

            var tcs = new TaskCompletionSource();

            dispatcherQueueController!.DispatcherQueue.TryEnqueue(() =>
            {
                try
                {
                    lock (hotkeyEventHandlers)
                    {
                        foreach (var (_, key) in hotkeyEventHandlers)
                        {
                            User32.UnregisterHotKey(messageWindowHandle, key);
                        }

                        hotkeyEventHandlers.Clear();
                    }
                }
                finally
                {
                    tcs.TrySetResult();
                }
            });

            await tcs.Task.ConfigureAwait(false);
        }

        public bool GetHotKeyState(User32.HotKeyModifiers modifiers, User32.VK key)
        {
            lock (hotkeyEventHandlers)
            {
                return hotkeyEventHandlers.ContainsKey((modifiers, key));
            }
        }

        public event HotKeyInvokedEventHandler? HotKeyInvoked;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    lock (hotkeyEventHandlers)
                    {
                        hotkeyEventHandlers.Clear();
                    }

                    initializeWaitHandle?.WaitOne();
                    initializeWaitHandle?.Dispose();
                    initializeWaitHandle = null;

                    messageWindowHandle?.Dispose();

                    dispatcherQueueController?.DispatcherQueue.TryEnqueue(
                        Windows.System.DispatcherQueuePriority.Low,
                        () => User32.PostQuitMessage(0));

                    dispatcherQueueController = null;

                    backgroundThread.Join();
                    backgroundThread = null!;

                    // TODO: 释放托管状态(托管对象)
                }

                // TODO: 释放未托管的资源(未托管的对象)并重写终结器
                // TODO: 将大型字段设置为 null
                disposedValue = true;
            }
        }

        // // TODO: 仅当“Dispose(bool disposing)”拥有用于释放未托管资源的代码时才替代终结器
        // ~HotKeyHelper()
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

    public record HotKeyInvokedEventArgs(User32.HotKeyModifiers Modifier, User32.VK Key);


    internal delegate void HotKeyInvokedEventHandler(HotKeyListener sender, HotKeyInvokedEventArgs args);
}
