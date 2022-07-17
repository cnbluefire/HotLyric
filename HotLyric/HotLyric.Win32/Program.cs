using HotLyric.Win32.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HotLyric.Win32
{
    public class Program
    {
        [STAThread]
        public static void Main()
        {
            if (!SingleInstance.IsMainInstance)
            {
                SingleInstance.ActiveMainInstance();
                return;
            }

            AppContext.SetSwitch("Switch.System.Windows.Input.Stylus.EnablePointerSupport", true);

            var app = new App();
            app.InitializeComponent();
            app.Run();
        }
    }
}
