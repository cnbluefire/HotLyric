using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Vanara.PInvoke;
using WinDispatcherQueueController = Windows.System.DispatcherQueueController;
using VirtualKey = Windows.System.VirtualKey;
using VirtualKeyModifiers = Windows.System.VirtualKeyModifiers;
using System.Runtime.CompilerServices;

namespace HotLyric.Win32.Utils
{
    internal class HotKeyHelper : IDisposable
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

        public HotKeyHelper()
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
            dispatcherQueueController.DispatcherQueue.ShutdownCompleted += DispatcherQueue_ShutdownCompleted;

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

        public bool GetHotKeyState(User32.HotKeyModifiers modifiers, User32.VK key)
        {
            lock (hotkeyEventHandlers)
            {
                return hotkeyEventHandlers.ContainsKey((modifiers, key));
            }
        }

        public event HotKeyInvokedEventHandler? HotKeyInvoked;

        private void DispatcherQueue_ShutdownCompleted(Windows.System.DispatcherQueue sender, object args)
        {
            User32.PostQuitMessage(0);
        }

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
                        () => dispatcherQueueController?.ShutdownQueueAsync());

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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static VirtualKeyModifiers GetCurrentVirtualModifiersStates()
        {
            VirtualKeyModifiers modifiers = default;

            if ((Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Control) & Windows.UI.Core.CoreVirtualKeyStates.Down) == Windows.UI.Core.CoreVirtualKeyStates.Down)
                modifiers |= VirtualKeyModifiers.Control;

            if ((Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.LeftWindows) & Windows.UI.Core.CoreVirtualKeyStates.Down) == Windows.UI.Core.CoreVirtualKeyStates.Down)
                modifiers |= VirtualKeyModifiers.Windows;

            if ((Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.RightWindows) & Windows.UI.Core.CoreVirtualKeyStates.Down) == Windows.UI.Core.CoreVirtualKeyStates.Down)
                modifiers |= VirtualKeyModifiers.Windows;

            if ((Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Menu) & Windows.UI.Core.CoreVirtualKeyStates.Down) == Windows.UI.Core.CoreVirtualKeyStates.Down)
                modifiers |= VirtualKeyModifiers.Menu;

            if ((Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Shift) & Windows.UI.Core.CoreVirtualKeyStates.Down) == Windows.UI.Core.CoreVirtualKeyStates.Down)
                modifiers |= VirtualKeyModifiers.Shift;

            return modifiers;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static User32.HotKeyModifiers GetCurrentModifiersStates() =>
            MapModifiers(GetCurrentVirtualModifiersStates());

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static VirtualKeyModifiers MapModifiers(User32.HotKeyModifiers modifiers)
        {
            VirtualKeyModifiers m = default;

            if ((modifiers & User32.HotKeyModifiers.MOD_CONTROL) != 0) m |= VirtualKeyModifiers.Control;
            if ((modifiers & User32.HotKeyModifiers.MOD_ALT) != 0) m |= VirtualKeyModifiers.Menu;
            if ((modifiers & User32.HotKeyModifiers.MOD_SHIFT) != 0) m |= VirtualKeyModifiers.Shift;
            if ((modifiers & User32.HotKeyModifiers.MOD_WIN) != 0) m |= VirtualKeyModifiers.Windows;

            return m;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static User32.HotKeyModifiers MapModifiers(VirtualKeyModifiers modifiers)
        {
            User32.HotKeyModifiers m = default;

            if ((modifiers & VirtualKeyModifiers.Control) != 0) m |= User32.HotKeyModifiers.MOD_CONTROL;
            if ((modifiers & VirtualKeyModifiers.Menu) != 0) m |= User32.HotKeyModifiers.MOD_ALT;
            if ((modifiers & VirtualKeyModifiers.Shift) != 0) m |= User32.HotKeyModifiers.MOD_SHIFT;
            if ((modifiers & VirtualKeyModifiers.Windows) != 0) m |= User32.HotKeyModifiers.MOD_WIN;

            return m;
        }
    }

    public record HotKeyInvokedEventArgs(User32.HotKeyModifiers Modifier, User32.VK Key);


    internal delegate void HotKeyInvokedEventHandler(HotKeyHelper sender, HotKeyInvokedEventArgs args);
}
