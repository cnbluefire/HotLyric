using System;
using System.Collections.Generic;
using System.Text;

namespace HotLyric.Win32.Utils
{
    public class MouseManagerFactory : IDisposable
    {
        private static object locker = new object();
        private static volatile int count;

        private bool disposeValue;

        public MouseManagerFactory()
        {
            lock (locker)
            {
                if (count == 0)
                {
                    Input.MouseManager.Install(useMouseHook: false);
                }
                count++;
            }
        }

        public void Dispose()
        {
            lock (locker)
            {
                if (!disposeValue)
                {
                    disposeValue = true;
                    count--;
                    if (count <= 0)
                    {
                        count = 0;
                        Input.MouseManager.Uninstall();
                    }
                }
            }
        }

        ~MouseManagerFactory()
        {
            Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
