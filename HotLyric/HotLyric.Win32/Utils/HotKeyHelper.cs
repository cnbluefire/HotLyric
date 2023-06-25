using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Vanara.PInvoke;
using VirtualKey = Windows.System.VirtualKey;
using VirtualKeyModifiers = Windows.System.VirtualKeyModifiers;


namespace HotLyric.Win32.Utils
{
    public static class HotKeyHelper
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
        private static Dictionary<User32.HotKeyModifiers, User32.VK[]> modifierKeys = new Dictionary<User32.HotKeyModifiers, User32.VK[]>();

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

                        if (key == User32.VK.VK_OEM_3 && name == "`")
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

        public static string MapKeyToString(User32.HotKeyModifiers modifiers, User32.VK key, bool compact = false)
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
                        sb.Append(item);

                        if (compact) sb.Append("+");
                        else sb.Append(" + ");
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


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static VirtualKeyModifiers GetCurrentVirtualKeyModifiersStates()
        {
            VirtualKeyModifiers modifiers = default;

            if ((Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Control) & Windows.UI.Core.CoreVirtualKeyStates.Down) == Windows.UI.Core.CoreVirtualKeyStates.Down)
                modifiers |= VirtualKeyModifiers.Control;

            if ((Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.LeftWindows) & Windows.UI.Core.CoreVirtualKeyStates.Down) == Windows.UI.Core.CoreVirtualKeyStates.Down)
                modifiers |= VirtualKeyModifiers.Windows;

            if ((Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.RightWindows) & Windows.UI.Core.CoreVirtualKeyStates.Down) == Windows.UI.Core.CoreVirtualKeyStates.Down)
                modifiers |= VirtualKeyModifiers.Windows;

            if ((Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Menu) & Windows.UI.Core.CoreVirtualKeyStates.Down) == Windows.UI.Core.CoreVirtualKeyStates.Down)
                modifiers |= VirtualKeyModifiers.Menu;

            if ((Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Shift) & Windows.UI.Core.CoreVirtualKeyStates.Down) == Windows.UI.Core.CoreVirtualKeyStates.Down)
                modifiers |= VirtualKeyModifiers.Shift;

            return modifiers;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static User32.HotKeyModifiers GetCurrentModifiersStates() =>
            MapModifiers(GetCurrentVirtualKeyModifiersStates());

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static VirtualKeyModifiers MapModifiers(User32.HotKeyModifiers modifiers)
        {
            VirtualKeyModifiers m = default;

            if ((modifiers & User32.HotKeyModifiers.MOD_CONTROL) != 0) m |= VirtualKeyModifiers.Control;
            if ((modifiers & User32.HotKeyModifiers.MOD_ALT) != 0) m |= VirtualKeyModifiers.Menu;
            if ((modifiers & User32.HotKeyModifiers.MOD_SHIFT) != 0) m |= VirtualKeyModifiers.Shift;
            if ((modifiers & User32.HotKeyModifiers.MOD_WIN) != 0) m |= VirtualKeyModifiers.Windows;

            return m;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static User32.HotKeyModifiers MapModifiers(VirtualKeyModifiers modifiers)
        {
            User32.HotKeyModifiers m = default;

            if ((modifiers & VirtualKeyModifiers.Control) != 0) m |= User32.HotKeyModifiers.MOD_CONTROL;
            if ((modifiers & VirtualKeyModifiers.Menu) != 0) m |= User32.HotKeyModifiers.MOD_ALT;
            if ((modifiers & VirtualKeyModifiers.Shift) != 0) m |= User32.HotKeyModifiers.MOD_SHIFT;
            if ((modifiers & VirtualKeyModifiers.Windows) != 0) m |= User32.HotKeyModifiers.MOD_WIN;

            return m;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static User32.HotKeyModifiers MapModifiers(User32.VK key) => key switch
        {
            User32.VK.VK_CONTROL => User32.HotKeyModifiers.MOD_CONTROL,
            User32.VK.VK_LCONTROL => User32.HotKeyModifiers.MOD_CONTROL,
            User32.VK.VK_RCONTROL => User32.HotKeyModifiers.MOD_CONTROL,

            User32.VK.VK_MENU => User32.HotKeyModifiers.MOD_ALT,
            User32.VK.VK_LMENU => User32.HotKeyModifiers.MOD_ALT,
            User32.VK.VK_RMENU => User32.HotKeyModifiers.MOD_ALT,

            User32.VK.VK_LWIN => User32.HotKeyModifiers.MOD_WIN,
            User32.VK.VK_RWIN => User32.HotKeyModifiers.MOD_WIN,

            User32.VK.VK_SHIFT => User32.HotKeyModifiers.MOD_SHIFT,
            User32.VK.VK_LSHIFT => User32.HotKeyModifiers.MOD_SHIFT,
            User32.VK.VK_RSHIFT => User32.HotKeyModifiers.MOD_SHIFT,

            _ => 0
        };

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static VirtualKeyModifiers MapVirtualKeyModifiers(User32.VK key) => MapModifiers(MapModifiers(key));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IReadOnlyList<User32.VK> MapModifiersToVirtualKey(User32.HotKeyModifiers modifiers)
        {
            if (modifiers == 0 || modifiers == User32.HotKeyModifiers.MOD_NOREPEAT) return Array.Empty<User32.VK>();

            lock (modifierKeys)
            {
                if (modifierKeys.TryGetValue(modifiers, out var keys)) return keys;

                var list = new List<User32.VK>();

                if ((modifiers & User32.HotKeyModifiers.MOD_CONTROL) != 0) list.Add(User32.VK.VK_CONTROL);
                if ((modifiers & User32.HotKeyModifiers.MOD_ALT) != 0) list.Add(User32.VK.VK_MENU);
                if ((modifiers & User32.HotKeyModifiers.MOD_SHIFT) != 0) list.Add(User32.VK.VK_SHIFT);
                if ((modifiers & User32.HotKeyModifiers.MOD_WIN) != 0) list.Add(User32.VK.VK_LWIN);

                if (list.Count > 0)
                {
                    keys = list.ToArray();
                }
                else
                {
                    keys = Array.Empty<User32.VK>();
                }
                modifierKeys[modifiers] = keys;
                return keys;
            }
        }

        public static bool SendKey(User32.VK key, bool keyUp)
        {
            var inputs = new User32.INPUT[1]
            {
                new User32.INPUT(keyUp ? User32.KEYEVENTF.KEYEVENTF_KEYUP : 0, (ushort)key)
            };
            return User32.SendInput(1, inputs, System.Runtime.InteropServices.Marshal.SizeOf<User32.INPUT>()) != 0;
        }
    }
}
