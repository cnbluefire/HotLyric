using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Vanara.PInvoke;
using Windows.Storage;

namespace HotLyric.Win32.Utils
{
    public static class SingleInstance
    {
        internal const string ActivateMessage = "097c7621-30b9-4524-9f38-ce1a0874dc07";

        private static Mutex? mutex;
        private static bool? isMainInstance;
        private static object locker = new object();

        public static bool IsMainInstance
        {
            get
            {
                if (!isMainInstance.HasValue)
                {
                    lock (locker)
                    {
                        if (!isMainInstance.HasValue)
                        {
                            mutex = new Mutex(true, "HotScreen.Main", out var createdNew);
                            isMainInstance = createdNew;
                            if (!createdNew)
                            {
                                mutex.Dispose();
                                mutex = null;
                            }
                        }
                    }
                }

                return isMainInstance.Value;
            }
        }

        internal static void TryReleaseMutex()
        {
            mutex?.Dispose();
            mutex = null;

            isMainInstance = false;
        }

        internal static void ActiveMainInstance()
        {
            try
            {
                List<IntPtr> windows = new List<IntPtr>();
                var proc = new User32.EnumWindowsProc((hwnd, lParam) =>
                {
                    if (!hwnd.IsNull)
                    {
                        var handle = hwnd.DangerousGetHandle();
                        var sb = new StringBuilder(256);
                        var len = User32.GetWindowText(handle, sb, 255);

                        if (len > 0 && sb.ToString() == "HostWindow")
                        {
                            try
                            {
                                if (User32.GetWindowThreadProcessId(hwnd, out var processId) > 0 && processId > 0)
                                {
                                    var process = Process.GetProcessById((int)processId);
                                    if (process.ProcessName == "HotLyric.Win32")
                                    {
                                        windows.Add(hwnd.DangerousGetHandle());
                                    }
                                }
                            }
                            catch { }
                        }
                    }

                    return true;
                });

                User32.EnumWindows(proc, IntPtr.Zero);

                foreach (var hwnd in windows)
                {
                    var sb = new StringBuilder(ActivateMessage);
                    var args = Environment.GetCommandLineArgs();

                    if (args.Length > 1)
                    {
                        for (int i = 1; i < args.Length; i++)
                        {
                            sb.Append('"').Append(args[i]).Append('"').Append(" ");
                        }
                    }

                    WindowHelper.SendCopyDataMessage(hwnd, sb.ToString());
                }
            }
            catch { }
        }
    }
}
