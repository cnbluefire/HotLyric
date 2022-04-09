using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace HotLyric.Win32.Utils
{
    internal class DispatcherHelper
    {
        public static Dispatcher? UIDispatcher { get; private set; }

        public static void Initialize(Dispatcher dispatcher)
        {
            if(UIDispatcher != null) throw new ArgumentException(nameof(UIDispatcher));

            UIDispatcher = dispatcher;
        }
    }
}
