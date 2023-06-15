using Microsoft.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Composition;
using Windows.UI.Composition.Desktop;
using WinRT;
using WinCompositor = Windows.UI.Composition.Compositor;
using WinDispatcherQueueController = Windows.System.DispatcherQueueController;
using WinDispatcherQueue = Windows.System.DispatcherQueue;
using Vanara.PInvoke;

namespace HotLyric.Win32.Utils
{
    public static class WindowsCompositionHelper
    {
        private static WinCompositor? compositor;
        private static WinDispatcherQueueController? dispatcherQueueController;
        private static object locker = new object();

        public static WinCompositor Compositor => EnsureCompositor();

        public static WinDispatcherQueue DispatcherQueue
        {
            get
            {
                EnsureCompositor();
                return dispatcherQueueController!.DispatcherQueue;
            }
        }

        private static WinCompositor EnsureCompositor()
        {
            if (compositor == null)
            {
                lock (locker)
                {
                    if (compositor == null)
                    {
                        var handle = new System.Threading.EventWaitHandle(false, System.Threading.EventResetMode.AutoReset);

                        // 在子线程创建Compositor
                        dispatcherQueueController = WinDispatcherQueueController.CreateOnDedicatedThread();
                        dispatcherQueueController.DispatcherQueue.TryEnqueue(Windows.System.DispatcherQueuePriority.High, () =>
                        {
                            compositor = new WinCompositor();
                            handle.Set();
                        });

                        handle.WaitOne();
                    }
                }
            }

            return compositor!;
        }
    }
}
