using BlueFire.Toolkit.WinUI3.WindowBase;
using HotLyric.Win32.Utils;
using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Vanara.PInvoke;

namespace HotLyric.Win32
{
    internal class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            var instance = Microsoft.Windows.AppLifecycle.AppInstance.FindOrRegisterForKey("HotLyric_BEC7FAA5-5F6F-4A8D-AC14-79048C8F214B");
            var activatedArgs = Microsoft.Windows.AppLifecycle.AppInstance.GetCurrent().GetActivatedEventArgs();
            
            if (instance.IsCurrent)
            {
                instance.Activated += App.Instance_Activated;
                ActivationArgumentsHelper.ProcessArguments(activatedArgs);
            }
            else
            {
                Task.Run(async () =>
                {
                    await instance.RedirectActivationToAsync(activatedArgs);
                }).Wait();

                return;
            }

            CultureInfoUtils.Initialize();

            XamlCheckProcessRequirements();

            global::WinRT.ComWrappersSupport.InitializeComWrappers();
            WindowManager.Initialize();

            global::Microsoft.UI.Xaml.Application.Start((p) =>
            {
                var context = new global::Microsoft.UI.Dispatching.DispatcherQueueSynchronizationContext(global::Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread());
                global::System.Threading.SynchronizationContext.SetSynchronizationContext(context);
                new App();
            });
        }


        [DllImport("Microsoft.ui.xaml.dll")]
        private static extern void XamlCheckProcessRequirements();
    }
}