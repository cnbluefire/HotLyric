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

        private static WinDispatcherQueueController InitializeCoreDispatcher()
        {
            DispatcherQueueOptions options = new DispatcherQueueOptions();
            options.apartmentType = DISPATCHERQUEUE_THREAD_APARTMENTTYPE.DQTAT_COM_STA;
            options.threadType = DISPATCHERQUEUE_THREAD_TYPE.DQTYPE_THREAD_CURRENT;
            options.dwSize = Marshal.SizeOf(typeof(DispatcherQueueOptions));

            CreateDispatcherQueueController(options, out var raw);

            return WinDispatcherQueueController.FromAbi(raw);
        }


        [Guid("AF86E2E0-B12D-4c6a-9C5A-D7AA65101E90")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IInspectable
        {
            void GetIids();
            int GetRuntimeClassName([Out, MarshalAs(UnmanagedType.HString)] out string name);
            void GetTrustLevel();
        }

        [ComImport]
        [Guid("29E691FA-4567-4DCA-B319-D0F207EB6807")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        public interface ICompositorDesktopInterop
        {
            void CreateDesktopWindowTarget(IntPtr hwndTarget, bool isTopmost, out IntPtr test);
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
        private static extern IntPtr CreateDispatcherQueueController(DispatcherQueueOptions options, out IntPtr dispatcherQueueController);
    }
}
