using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Vanara.PInvoke;

namespace HotLyric.Win32.Utils
{
    internal static class SystemEventsHelper
    {
        private static BasicMessageWindow? messageWindow;
        private static BasicMessageWindowFilter? messageFilter;
        private static Dictionary<string, Delegate> events = new Dictionary<string, Delegate>();


        public static event EventHandler? EndSession
        {
            add => AddHandle(value);
            remove => RemoveHandle(value);
        }

        private static void EnsureMessageWindow()
        {
            if (messageWindow == null)
            {
                lock (events)
                {
                    if (messageWindow == null)
                    {
                        messageFilter = new BasicMessageWindowFilter(WndProc);
                        messageWindow = new BasicMessageWindow(messageFilter);
                    }
                }
            }
        }

        private static bool WndProc(HWND hwnd, uint msg, nint wParam, nint lParam, out nint lReturn)
        {
            lReturn = 0;

            var message = (User32.WindowMessage)msg;
            LogHelper.LogInfo(message.ToString());

            if (message == User32.WindowMessage.WM_QUERYENDSESSION)
            {
                lReturn = 1;
                return true;
            }
            else if (message == User32.WindowMessage.WM_ENDSESSION)
            {
                if (wParam == 1)
                {
                    App.DispatcherQueue.TryEnqueue(() =>
                    {
                        try
                        {
                            TryGetEvent<EventHandler>(nameof(EndSession))?.Invoke(null, EventArgs.Empty);
                        }
                        catch { }
                    });
                    return true;
                }
            }

            return false;
        }

        private static void AddHandle(Delegate? @delegate, [CallerMemberName] string eventName = "")
        {
            if (@delegate == null || string.IsNullOrEmpty(eventName)) return;

            lock (events)
            {
                EnsureMessageWindow();

                if (events.TryGetValue(eventName, out var eventHandle))
                {
                    @delegate = Delegate.Combine(eventHandle, @delegate);
                }

                events[eventName] = @delegate;
            }
        }


        private static void RemoveHandle(Delegate? @delegate, [CallerMemberName] string eventName = "")
        {
            if (@delegate == null || string.IsNullOrEmpty(eventName)) return;

            lock (events)
            {
                Delegate? d = null;
                if (events.TryGetValue(eventName, out var eventHandle))
                {
                    d = Delegate.Remove(eventHandle, @delegate);
                }

                if (d != null)
                {
                    events[eventName] = @delegate;
                }
                else
                {
                    events.Remove(eventName);
                }
            }
        }

        private static T? TryGetEvent<T>(string eventName) where T : Delegate
        {
            if (string.IsNullOrEmpty(eventName)) return null;

            lock (events)
            {
                if (events.TryGetValue(eventName, out var value)
                    && value is T value2)
                {
                    return value2;
                }
            }

            return null;
        }

    }
}
