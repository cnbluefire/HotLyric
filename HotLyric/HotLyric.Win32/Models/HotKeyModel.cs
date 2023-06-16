using CommunityToolkit.Mvvm.ComponentModel;
using HotLyric.Win32.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Vanara.PInvoke;
using VirtualKey = Windows.System.VirtualKey;
using VirtualKeyModifiers = Windows.System.VirtualKeyModifiers;

namespace HotLyric.Win32.Models
{
    public partial class HotKeyModel : ObservableObject
    {
        public HotKeyModel(string hotKeyName)
        {
            HotKeyName = hotKeyName;
        }

        [ObservableProperty]
        private User32.HotKeyModifiers modifiers;

        [ObservableProperty]
        private User32.VK key;

        [ObservableProperty]
        private bool isEnabled;

        public string HotKeyName { get; }

        public void Update(VirtualKeyModifiers modifiers, VirtualKey key)
        {
            this.Key = (User32.VK)key;
            User32.HotKeyModifiers m = default;

            if ((modifiers & VirtualKeyModifiers.Control) != 0) m |= User32.HotKeyModifiers.MOD_CONTROL;
            if ((modifiers & VirtualKeyModifiers.Menu) != 0) m |= User32.HotKeyModifiers.MOD_ALT;
            if ((modifiers & VirtualKeyModifiers.Windows) != 0) m |= User32.HotKeyModifiers.MOD_WIN;
            if ((modifiers & VirtualKeyModifiers.Shift) != 0) m |= User32.HotKeyModifiers.MOD_SHIFT;

            this.Modifiers = m;
        }

        public override string ToString() => HotKeyHelper.MapKeyToString(Modifiers, Key);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int ToSettingValue()
        {
            return BuildSettingValue(Modifiers, Key);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static HotKeyModel CreateFromSettingValue(string hotKeyName, int settingValue)
        {
            var (modifiers, key) = GetKeyFromSettingValue(settingValue);

            return new HotKeyModel(hotKeyName)
            {
                Key = key,
                Modifiers = modifiers
            };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int BuildSettingValue(User32.HotKeyModifiers modifiers, User32.VK key)
        {
            return (int)modifiers << 16 | (int)key;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static (User32.HotKeyModifiers modifiers, User32.VK key) GetKeyFromSettingValue(int settingValue)
        {
            var modifiers = (User32.HotKeyModifiers)(settingValue >> 16);
            var key = (User32.VK)(settingValue & 0xFFFF);

            return (modifiers, key);
        }
    }
}
