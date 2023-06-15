using CommunityToolkit.Mvvm.ComponentModel;
using HotLyric.Win32.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vanara.PInvoke;

namespace HotLyric.Win32.Models
{
    internal class HotKeyManager : ObservableObject
    {
        private HotKeyHelper? hotKeyHelper;

        public async Task InstallAsync()
        {
            lock (this)
            {
                if (hotKeyHelper != null)
                {
                    hotKeyHelper = new HotKeyHelper();
                }
            }

            await RefreshAsync();
        }

        public void Uninstall()
        {
            if (hotKeyHelper != null)
            {
                lock (this)
                {
                    hotKeyHelper?.Dispose();
                    hotKeyHelper = null;
                }
            }
        }

        public async Task RefreshAsync()
        {

        }

    }
}
