using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;

namespace HotLyric.Win32.Themes
{
    public class SystemResourceOverrideDictionary : ResourceDictionary
    {
        public SystemResourceOverrideDictionary()
        {
            if (Environment.OSVersion.Version >= new Version(10, 0, 22000, 0))
            {
                this.Source = new Uri("ms-appx:///Themes/SystemResourceOverrideDictionaryWin11.xaml");
            }
            else
            {
                this.Source = new Uri("ms-appx:///Themes/SystemResourceOverrideDictionaryWin10.xaml");
            }
        }
    }
}
