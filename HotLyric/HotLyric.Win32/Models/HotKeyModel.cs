using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vanara.PInvoke;
using VirtualKey = Windows.System.VirtualKey;
using VirtualKeyModifiers = Windows.System.VirtualKeyModifiers;

namespace HotLyric.Win32.Models
{
    internal partial class HotKeyModel : ObservableObject
    {
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(KeyText))]
        [NotifyPropertyChangedFor(nameof(Completed))]
        private User32.HotKeyModifiers modifiers;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(KeyText))]
        [NotifyPropertyChangedFor(nameof(Completed))]
        private User32.VK key;

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

        public string KeyText => KeyToStringConverter.MapKeyToString(Modifiers, Key);

        public bool Completed => KeyToStringConverter.IsCompleted(Modifiers, Key);

        public override string ToString() => KeyText;

        private static class KeyToStringConverter
        {
            private static Dictionary<User32.VK, string> keyNames = new Dictionary<User32.VK, string>()
            {
                [User32.VK.VK_PRIOR] = "Page Up",
                [User32.VK.VK_NEXT] = "Page Down",
                [User32.VK.VK_HOME] = "Home",
                [User32.VK.VK_END] = "End",
                [User32.VK.VK_LEFT] = "←",
                [User32.VK.VK_RIGHT] = "→",
                [User32.VK.VK_UP] = "↑",
                [User32.VK.VK_DOWN] = "↓",

                [User32.VK.VK_MULTIPLY] = "Num *",
                [User32.VK.VK_ADD] = "Num +",
                [User32.VK.VK_SUBTRACT] = "Num -",
                [User32.VK.VK_DECIMAL] = "Num .",
                [User32.VK.VK_DIVIDE] = "Num /",

                //[User32.VK.VK_OEM_PLUS] = "+",
                //[User32.VK.VK_OEM_COMMA] = ",",
                //[User32.VK.VK_OEM_MINUS] = "-",
                //[User32.VK.VK_OEM_PERIOD] = ".",
                //[User32.VK.VK_OEM_1] = ";",
                //[User32.VK.VK_OEM_2] = "?",
                //[User32.VK.VK_OEM_3] = "~",
                //[User32.VK.VK_OEM_4] = "[",
                //[User32.VK.VK_OEM_5] = "\\",
                //[User32.VK.VK_OEM_6] = "]",
                //[User32.VK.VK_OEM_7] = "'",
            };

            private static Dictionary<User32.HotKeyModifiers, string[]> modifierNames = new Dictionary<User32.HotKeyModifiers, string[]>();

            public static string[] MapKeyToString(User32.HotKeyModifiers modifiers)
            {
                User32.HotKeyModifiers modifiers2 = default;

                int capacity = 0;

                if ((modifiers & User32.HotKeyModifiers.MOD_CONTROL) != 0)
                {
                    capacity++;
                    modifiers2 |= User32.HotKeyModifiers.MOD_CONTROL;
                }
                if ((modifiers & User32.HotKeyModifiers.MOD_ALT) != 0)
                {
                    capacity++;
                    modifiers2 |= User32.HotKeyModifiers.MOD_ALT;
                }
                if ((modifiers & User32.HotKeyModifiers.MOD_SHIFT) != 0)
                {
                    capacity++;
                    modifiers2 |= User32.HotKeyModifiers.MOD_SHIFT;
                }
                if ((modifiers & User32.HotKeyModifiers.MOD_WIN) != 0)
                {
                    capacity++;
                    modifiers2 |= User32.HotKeyModifiers.MOD_WIN;
                }

                if (capacity == 0) return Array.Empty<string>();

                lock (modifierNames)
                {
                    if (modifierNames.TryGetValue(modifiers2, out var value)) return value;

                    value = new string[capacity];
                    var idx = 0;

                    if ((modifiers2 & User32.HotKeyModifiers.MOD_CONTROL) != 0) value[idx++] = "Ctrl";
                    if ((modifiers2 & User32.HotKeyModifiers.MOD_ALT) != 0) value[idx++] = "Alt";
                    if ((modifiers2 & User32.HotKeyModifiers.MOD_SHIFT) != 0) value[idx++] = "Shift";
                    if ((modifiers2 & User32.HotKeyModifiers.MOD_WIN) != 0) value[idx++] = "Win";

                    return value;
                }

            }

            public static string MapKeyToString(User32.VK key)
            {
                lock (keyNames)
                {
                    if (keyNames.TryGetValue(key, out var name)) return name;

                    else if (key == User32.VK.VK_OEM_PLUS        // 加号 +
                        || key == User32.VK.VK_OEM_COMMA    // 逗号 ,
                        || key == User32.VK.VK_OEM_MINUS    // 减号 -
                        || key == User32.VK.VK_OEM_PERIOD   // 句点 .
                        || key == User32.VK.VK_OEM_1        // 分号 ;
                        || key == User32.VK.VK_OEM_2        // 问号 ?
                        || key == User32.VK.VK_OEM_3        // 波浪线号 ~
                        || key == User32.VK.VK_OEM_4        // 左中括号 [
                        || key == User32.VK.VK_OEM_5        // 反斜线 \
                        || key == User32.VK.VK_OEM_6        // 右中括号 ]
                        || key == User32.VK.VK_OEM_7)       // 单引号 '
                    {
                        var sb = new StringBuilder(64);
                        var scanCode = User32.MapVirtualKey((uint)key, User32.MAPVK.MAPVK_VK_TO_VSC);

                        User32.GetKeyNameText((int)(scanCode << 16), sb, 64);

                        if (sb.Length > 0)
                        {
                            name = sb.ToString();

                            if(key == User32.VK.VK_OEM_3 && name == "`")
                            {
                                name = "~";
                            }
                        }
                        else
                        {
                            if (key == User32.VK.VK_OEM_PLUS) name = "+";
                            else if (key == User32.VK.VK_OEM_COMMA) name = ",";
                            else if (key == User32.VK.VK_OEM_MINUS) name = "-";
                            else if (key == User32.VK.VK_OEM_PERIOD) name = ".";
                            else if (key == User32.VK.VK_OEM_1) name = ";";
                            else if (key == User32.VK.VK_OEM_2) name = "?";
                            else if (key == User32.VK.VK_OEM_3) name = "~";
                            else if (key == User32.VK.VK_OEM_4) name = "[";
                            else if (key == User32.VK.VK_OEM_5) name = "\\";
                            else if (key == User32.VK.VK_OEM_6) name = "]";
                            else if (key == User32.VK.VK_OEM_7) name = "'";
                        }

                        keyNames[key] = name!;
                        return name!;
                    }
                }

                var keyNum = (int)key;

                if (keyNum >= (int)User32.VK.VK_0 && keyNum <= (int)User32.VK.VK_9)
                {
                    var ch = (char)(keyNum - (int)User32.VK.VK_0 + '0');
                    return $"{ch}";
                }

                else if (keyNum >= (int)User32.VK.VK_A && keyNum <= (int)User32.VK.VK_Z)
                {
                    var ch = (char)(keyNum - (int)User32.VK.VK_A + 'A');
                    return $"{ch}";
                }

                else if (keyNum >= (int)User32.VK.VK_NUMPAD0 && keyNum <= (int)User32.VK.VK_NUMPAD9)
                {
                    var ch = (char)(keyNum - (int)User32.VK.VK_NUMPAD0 + '0');
                    return $"Num {ch}";
                }

                return "";
            }

            public static bool IsCompleted(User32.HotKeyModifiers modifiers, User32.VK? key)
            {
                if (!key.HasValue) return false;

                var modifierTexts = MapKeyToString(modifiers);

                if (modifierTexts == null || modifierTexts.Length == 0) return false;

                var keyText = MapKeyToString(key.Value);

                if (string.IsNullOrEmpty(keyText)) return false;

                return true;
            }

            public static string MapKeyToString(User32.HotKeyModifiers modifiers, User32.VK key)
            {
                var modifierTexts = MapKeyToString(modifiers);
                var keyText = key != 0 ? MapKeyToString(key) : "";

                if ((modifierTexts != null && modifierTexts.Length > 0)
                    || !string.IsNullOrEmpty(keyText))
                {
                    var sb = new StringBuilder();

                    if (modifierTexts != null && modifierTexts.Length > 0)
                    {
                        foreach (var item in modifierTexts)
                        {
                            sb.Append(item)
                                .Append(" + ");
                        }
                    }

                    if (!string.IsNullOrEmpty(keyText))
                    {
                        sb.Append(keyText);
                    }
                    else if (sb.Length > 0)
                    {
                        sb.Length--;
                    }

                    return sb.ToString();
                }

                return "";
            }
        }
    }
}
