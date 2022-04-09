using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Vanara.PInvoke;
using Windows.System;

namespace HotLyric.Input
{
    public sealed class WorkerThread : IDisposable
    {
        private Thread thread;
        private volatile bool disposedValue;
        private EventWaitHandle? initlocker = new EventWaitHandle(false, EventResetMode.ManualReset);
        private DispatcherQueueController? queueController;

        public WorkerThread()
        {
            thread = new Thread(Loop);
            thread.IsBackground = true;
            thread.SetApartmentState(ApartmentState.STA);
        }

        public void Start()
        {
            if (disposedValue)
            {
                throw new ObjectDisposedException(nameof(WorkerThread));
            }
            thread.Start();
        }

        private void Loop()
        {
            InitializeCoreDispatcher();

            initlocker!.Set();

            while (!disposedValue && User32.GetMessage(out var msg))
            {
                User32.TranslateMessage(msg);
                User32.DispatchMessage(msg);
            }
        }

        private void InitializeCoreDispatcher()
        {
            DispatcherQueueOptions options = new DispatcherQueueOptions();
            options.apartmentType = DISPATCHERQUEUE_THREAD_APARTMENTTYPE.DQTAT_COM_STA;
            options.threadType = DISPATCHERQUEUE_THREAD_TYPE.DQTYPE_THREAD_CURRENT;
            options.dwSize = Marshal.SizeOf(typeof(DispatcherQueueOptions));

            if (CreateDispatcherQueueController(options, out var queue) != IntPtr.Zero)
            {
                throw new InvalidOperationException();
            }

            var obj = Marshal.GetObjectForIUnknown(queue);

            queueController = (DispatcherQueueController)((object)obj);
        }

        public Task RunAsync(Action callback)
        {
            return RunAsync(DispatcherQueuePriority.Normal, callback);
        }

        public async Task RunAsync(DispatcherQueuePriority priority, Action callback)
        {
            if (callback is null)
            {
                throw new ArgumentNullException(nameof(callback));
            }

            if (disposedValue)
            {
                throw new ObjectDisposedException(nameof(WorkerThread));
            }

            var tcs = new TaskCompletionSource<object?>();

            if (initlocker != null && initlocker.WaitOne())
            {
                initlocker?.Dispose();
                initlocker = null;
            }

            if (queueController == null)
            {
                throw new ArgumentNullException();
            }

            var action = new DispatcherQueueHandler(() =>
            {
                try
                {
                    if (disposedValue)
                    {
                        throw new ObjectDisposedException(nameof(WorkerThread));
                    }

                    callback.Invoke();
                    tcs.SetResult(null);
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                }
            });

            if (!queueController.DispatcherQueue.TryEnqueue(priority, action))
            {
                throw new InvalidOperationException();
            }

            await tcs.Task.ConfigureAwait(false);
        }

        private void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: 释放托管状态(托管对象)
                    var locker = new AutoResetEvent(false);

                    _ = queueController?.ShutdownQueueAsync().AsTask().ContinueWith(t =>
                    {
                        locker.Set();
                    });

                    locker.WaitOne();
                    locker.Dispose();
                    queueController = null;
                    initlocker?.Dispose();
                }

                // TODO: 释放未托管的资源(未托管的对象)并重写终结器
                // TODO: 将大型字段设置为 null
                disposedValue = true;
            }
        }

        // // TODO: 仅当“Dispose(bool disposing)”拥有用于释放未托管资源的代码时才替代终结器
        // ~WorkerThread()
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

        private enum DISPATCHERQUEUE_THREAD_APARTMENTTYPE
        {
            DQTAT_COM_NONE = 0,
            DQTAT_COM_ASTA = 1,
            DQTAT_COM_STA = 2
        };

        private enum DISPATCHERQUEUE_THREAD_TYPE
        {
            DQTYPE_THREAD_DEDICATED = 1,
            DQTYPE_THREAD_CURRENT = 2,
        };

        [StructLayout(LayoutKind.Sequential)]
        private struct DispatcherQueueOptions
        {
            public int dwSize;

            [MarshalAs(UnmanagedType.I4)]
            public DISPATCHERQUEUE_THREAD_TYPE threadType;

            [MarshalAs(UnmanagedType.I4)]
            public DISPATCHERQUEUE_THREAD_APARTMENTTYPE apartmentType;
        };

        [DllImport("coremessaging.dll", EntryPoint = "CreateDispatcherQueueController", CharSet = CharSet.Unicode)]
        private static extern IntPtr CreateDispatcherQueueController(
            DispatcherQueueOptions options,
            out IntPtr dispatcherQueueController);
    }
}
