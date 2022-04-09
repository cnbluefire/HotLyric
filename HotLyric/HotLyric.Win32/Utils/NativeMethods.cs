using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace HotLyric.Win32.Utils
{
    public static class NativeMethods
    {

        [StructLayout(LayoutKind.Sequential)]
        public struct STYLESTRUCT
        {
            public int styleOld;
            public int styleNew;
        }

    }
}
