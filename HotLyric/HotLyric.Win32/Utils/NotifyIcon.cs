using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Interop;

namespace HotLyric.Win32.Utils
{
    internal class NotifyIcon : IDisposable
    {
        private Hardcodet.Wpf.TaskbarNotification.TaskbarIcon taskbarIcon;
        private System.Drawing.Bitmap? icon;
        private System.Drawing.Icon? actualIcon;
        private System.Windows.Controls.ContextMenu? contextMenu;
        private bool disposedValue;

        public NotifyIcon()
        {
            taskbarIcon = new Hardcodet.Wpf.TaskbarNotification.TaskbarIcon()
            {
                MenuActivation = Hardcodet.Wpf.TaskbarNotification.PopupActivationMode.RightClick,
            };
            taskbarIcon.TrayLeftMouseDown += TaskbarIcon_TrayLeftMouseDown;
            taskbarIcon.TrayMouseDoubleClick += TaskbarIcon_TrayMouseDoubleClick;
            taskbarIcon.PreviewTrayContextMenuOpen += TaskbarIcon_PreviewTrayContextMenuOpen;
        }

        public System.Drawing.Bitmap? Icon
        {
            get => icon;
            set
            {
                if (icon != value)
                {
                    icon = value;
                    UpdateIcon();
                }
            }
        }

        public bool Visible
        {
            get => taskbarIcon.Visibility == System.Windows.Visibility.Visible;
            set => taskbarIcon.Visibility = value ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
        }

        public string ToolTipText
        {
            get => taskbarIcon.ToolTipText;
            set => taskbarIcon.ToolTipText = value;
        }

        public System.Windows.Controls.ContextMenu? ContextMenu
        {
            get => contextMenu;
            set
            {
                if (contextMenu != value)
                {
                    if (contextMenu != null)
                    {
                        contextMenu.IsOpen = false;
                    }

                    contextMenu = value;

                }

                taskbarIcon.ContextMenu = value;
            }
        }

        private void UpdateIcon()
        {
            var oldVis = Visible;
            Visible = false;

            taskbarIcon.Icon = null;
            if (actualIcon != null)
            {
                actualIcon.Dispose();
                actualIcon = null;
            }

            if (Icon != null)
            {
                var size = System.Windows.Forms.SystemInformation.IconSize * 2;
                using (var bitmap = new System.Drawing.Bitmap(Icon, size))
                {
                    actualIcon = System.Drawing.Icon.FromHandle(bitmap.GetHicon());
                    taskbarIcon.Icon = actualIcon;
                }
            }

            Visible = oldVis;
        }

        private void TaskbarIcon_PreviewTrayContextMenuOpen(object sender, RoutedEventArgs e)
        {
            var hostWindow = App.Current.Windows.OfType<Views.HostWindow>().FirstOrDefault();
            if (hostWindow != null)
            {
                var hwnd = new WindowInteropHelper(hostWindow).Handle;
                if (hwnd != IntPtr.Zero)
                {
                    var foregroundWindow = Vanara.PInvoke.User32.GetForegroundWindow();
                    if (!foregroundWindow.IsNull)
                    {
                        Vanara.PInvoke.User32.SetForegroundWindow(hwnd);
                        Vanara.PInvoke.User32.SetForegroundWindow(foregroundWindow);
                    }
                }
            }

        }

        private void TaskbarIcon_TrayMouseDoubleClick(object sender, System.Windows.RoutedEventArgs e)
        {
            OnDoubleClick();
        }

        private void TaskbarIcon_TrayLeftMouseDown(object sender, System.Windows.RoutedEventArgs e)
        {
            OnClick();
        }


        public event EventHandler? Click;
        public event EventHandler? DoubleClick;

        private void OnClick()
        {
            Click?.Invoke(this, EventArgs.Empty);
        }

        private void OnDoubleClick()
        {
            DoubleClick?.Invoke(this, EventArgs.Empty);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: 释放托管状态(托管对象)

                    if (taskbarIcon != null)
                    {
                        taskbarIcon.TrayLeftMouseDown -= TaskbarIcon_TrayLeftMouseDown;
                        taskbarIcon.TrayMouseDoubleClick -= TaskbarIcon_TrayMouseDoubleClick;
                        taskbarIcon.PreviewTrayContextMenuOpen -= TaskbarIcon_PreviewTrayContextMenuOpen;
                        taskbarIcon.Visibility = System.Windows.Visibility.Collapsed;
                        taskbarIcon.Icon = null;
                        taskbarIcon.Dispose();

                        Icon = null;
                    }
                }

                // TODO: 释放未托管的资源(未托管的对象)并重写终结器
                // TODO: 将大型字段设置为 null
                disposedValue = true;
            }
        }

        // // TODO: 仅当“Dispose(bool disposing)”拥有用于释放未托管资源的代码时才替代终结器
        // ~NotifyIcon()
        // {
        //     // 不要更改此代码。请将清理代码放入“Dispose(bool disposing)”方法中
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            // 不要更改此代码。请将清理代码放入“Dispose(bool disposing)”方法中
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
