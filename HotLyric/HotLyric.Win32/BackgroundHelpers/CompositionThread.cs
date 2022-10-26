using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.UI.Composition;
using Windows.UI.Composition.Desktop;
using System.Numerics;
using System.Diagnostics.CodeAnalysis;
using Windows.System;
using Vanara.PInvoke;
using WinRT;

namespace HotLyric.Win32.BackgroundHelpers
{
    public class CompositionThread
    {
        private static CompositionThread? instance;

        public static CompositionThread Instance
        {
            get
            {
                if (instance == null)
                {
                    lock (typeof(CompositionThread))
                    {
                        if (instance == null)
                        {
                            instance = new CompositionThread();
                        }
                    }
                }
                return instance!;
            }
        }

        private readonly Thread thread;
        private Compositor? compositor;
        private DispatcherQueueController? dispatcherQueueController;
        private object locker = new object();

        private CompositionThread()
        {
            thread = new Thread(MessageLoop);
            thread.SetApartmentState(ApartmentState.STA);
            thread.Name = "CompositionThread";
            thread.IsBackground = true;
            thread.Start();
            lock (locker)
            {
                Monitor.Wait(locker);
            }
        }

        public Compositor Compositor => compositor!;

        public DispatcherQueue DispatcherQueue => dispatcherQueueController!.DispatcherQueue;

        private void MessageLoop()
        {
            var options = new DispatcherQueueOptions();
            options.apartmentType = DISPATCHERQUEUE_THREAD_APARTMENTTYPE.DQTAT_COM_STA;
            options.threadType = DISPATCHERQUEUE_THREAD_TYPE.DQTYPE_THREAD_CURRENT;
            options.dwSize = (uint)Marshal.SizeOf(typeof(DispatcherQueueOptions));

            CreateDispatcherQueueController(options, out var pUnknown).ThrowIfFailed();

            var obj = Marshal.GetObjectForIUnknown(pUnknown);
            
            this.dispatcherQueueController = DispatcherQueueController.FromAbi(pUnknown);

            compositor = new Compositor();

            lock (locker)
            {
                Monitor.PulseAll(locker);
            }

            MSG Msg;

            while (User32.GetMessage(out Msg, (HWND)IntPtr.Zero, 0, 0))
            {
                User32.TranslateMessage(Msg);
                User32.DispatchMessage(Msg);
            }
        }

        public ContainerVisual CreateRootVisual(IntPtr hwndTarget, bool isTopmost)
        {            
            var interop = WinRT.CastExtensions.As<ICompositorDesktopInterop>(compositor);
            if (interop == null) throw new ArgumentException(nameof(interop));

            interop.CreateDesktopWindowTarget(hwndTarget, isTopmost, out var pUnknown);

            //var obj = Marshal.GetObjectForIUnknown(pUnknown);
            
            var target = DesktopWindowTarget.FromAbi(pUnknown);

            var root = compositor!.CreateContainerVisual();
            root.RelativeSizeAdjustment = new Vector2(1f, 1f);
            target.Root = root;

            return root;
        }

        [DllImport("coremessaging.dll", EntryPoint = "CreateDispatcherQueueController", CharSet = CharSet.Unicode)]
        private static extern HRESULT CreateDispatcherQueueController(DispatcherQueueOptions options,
                                        out IntPtr pDispatcherQueueController);

        [ComImport]
        [Guid("29E691FA-4567-4DCA-B319-D0F207EB6807")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface ICompositorDesktopInterop
        {
            void CreateDesktopWindowTarget(IntPtr hwndTarget, bool isTopmost, out IntPtr result);
        }

        struct DispatcherQueueOptions
        {
            /// <summary>Size of this <b>DispatcherQueueOptions</b> structure.</summary>
            internal uint dwSize;
            /// <summary>Thread affinity for the created <a href="https://docs.microsoft.com/uwp/api/windows.system.dispatcherqueuecontroller">DispatcherQueueController</a>.</summary>
            internal DISPATCHERQUEUE_THREAD_TYPE threadType;
            /// <summary>Specifies whether to initialize COM apartment on the new thread as an application single-threaded apartment (ASTA)  or single-threaded apartment (STA). This field is only relevant if <b>threadType</b> is <b>DQTYPE_THREAD_DEDICATED</b>. Use <b>DQTAT_COM_NONE</b> when <b>DispatcherQueueOptions.threadType</b> is <b>DQTYPE_THREAD_CURRENT</b>.</summary>
            internal DISPATCHERQUEUE_THREAD_APARTMENTTYPE apartmentType;
        }

        enum DISPATCHERQUEUE_THREAD_TYPE
        {
            /// <summary>Specifies that the <a href="https://docs.microsoft.com/uwp/api/windows.system.dispatcherqueuecontroller">DispatcherQueueController</a> be created on a dedicated thread. With this option, <a href="https://docs.microsoft.com/windows/desktop/api/dispatcherqueue/nf-dispatcherqueue-createdispatcherqueuecontroller">CreateDispatcherQueueController</a> creates a thread, the <a href="https://docs.microsoft.com/uwp/api/windows.system.dispatcherqueuecontroller">DispatcherQueueController</a> instance, and runs the dispatcher queue event loop on the newly created thread.</summary>
            DQTYPE_THREAD_DEDICATED = 1,            /// <summary>Specifies that the <a href="https://docs.microsoft.com/uwp/api/windows.system.dispatcherqueuecontroller">DispatcherQueueController</a> will be created on the caller's thread.</summary>
            DQTYPE_THREAD_CURRENT = 2,
        }

        enum DISPATCHERQUEUE_THREAD_APARTMENTTYPE
        {
            /// <summary>No COM threading apartment type specified.</summary>
            DQTAT_COM_NONE = 0,         /// <summary>Specifies an application single-threaded apartment (ASTA) COM threading apartment.</summary>
            DQTAT_COM_ASTA = 1,         /// <summary>Specifies a single-threaded apartment (STA) COM threading apartment.</summary>
            DQTAT_COM_STA = 2,
        }
    }
}
