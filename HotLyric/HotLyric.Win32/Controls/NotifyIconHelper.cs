using HotLyric.Win32.Utils;
using HotLyric.Win32.ViewModels;
using HotLyric.Win32.Views;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Threading;

namespace HotLyric.Win32.Controls
{
    public class NotifyIconHelper : IDisposable
    {
        private bool disposedValue;
        private NotifyIcon notifyIcon;

        private System.Windows.Controls.MenuItem launcherMenuItem;
        private System.Windows.Controls.MenuItem transparentMenuItem;
        private System.Windows.Controls.MenuItem alwaysShowBackgroundMenuItem;
        private System.Windows.Controls.MenuItem karaokeMenuItem;
        private System.Windows.Controls.MenuItem lyricHorizontalAlignmentMenuItem;
        private System.Windows.Controls.MenuItem settingsMenuItem;
        private System.Windows.Controls.MenuItem exitMenuItem;

        private System.Drawing.Bitmap? icon;
        private System.Drawing.Bitmap? iconDark;
        private DispatcherTimer themeChangedTimer;

        public NotifyIconHelper()
        {
            notifyIcon = new NotifyIcon();

            var contextMenu = new System.Windows.Controls.ContextMenu();

            contextMenu.AddHandler(System.Windows.Controls.MenuItem.ClickEvent, new RoutedEventHandler(ContextMenu_ItemClick));

            launcherMenuItem = new System.Windows.Controls.MenuItem()
            {
                Header = "使用帮助",
                Name = "ReadMe",
            };

            transparentMenuItem = new System.Windows.Controls.MenuItem()
            {
                Header = "锁定歌词",
                Name = "Transparent",
                IsCheckable = true,
            };

            alwaysShowBackgroundMenuItem = new System.Windows.Controls.MenuItem()
            {
                Header = "始终显示背景",
                Name = "AlwaysShowBackground",
                IsCheckable = true,
            };

            karaokeMenuItem = new System.Windows.Controls.MenuItem()
            {
                Header = "卡拉OK模式",
                Name = "Karaoke",
                IsCheckable = true,
                IsChecked = true,
            };

            lyricHorizontalAlignmentMenuItem = new System.Windows.Controls.MenuItem()
            {
                Header = "歌词对齐方式",
                Name = "HorizontalContentAlignment",
                Items =
                {
                    new System.Windows.Controls.MenuItem()
                    {
                        Header = "左对齐",
                        Name = "HorizontalContentAlignment_Left",
                        Tag = HorizontalAlignment.Left,
                        IsCheckable = true,
                        IsChecked = false,
                    },
                    new System.Windows.Controls.MenuItem()
                    {
                        Header = "居中",
                        Name = "HorizontalContentAlignment_Center",
                        Tag = HorizontalAlignment.Center,
                        IsCheckable = true,
                        IsChecked = false,
                    },
                    new System.Windows.Controls.MenuItem()
                    {
                        Header = "右对齐",
                        Name = "HorizontalContentAlignment_Right",
                        Tag = HorizontalAlignment.Right,
                        IsCheckable = true,
                        IsChecked = false,
                    },
                }
            };

            settingsMenuItem = new System.Windows.Controls.MenuItem()
            {
                Header = "设置",
                Name = "Settings"
            };

            exitMenuItem = new System.Windows.Controls.MenuItem()
            {
                Header = "退出",
                Name = "Exit"
            };

            contextMenu.Items.Add(launcherMenuItem);
            contextMenu.Items.Add(transparentMenuItem);
            contextMenu.Items.Add(alwaysShowBackgroundMenuItem);
            contextMenu.Items.Add(karaokeMenuItem);
            contextMenu.Items.Add(lyricHorizontalAlignmentMenuItem);

            contextMenu.Items.Add(new System.Windows.Controls.Separator());
            contextMenu.Items.Add(settingsMenuItem);

            contextMenu.Items.Add(new System.Windows.Controls.Separator());
            contextMenu.Items.Add(exitMenuItem);

            notifyIcon.ContextMenu = contextMenu;

            notifyIcon.Click += NotifyIcon_Click;
            notifyIcon.DoubleClick += NotifyIcon_DoubleClick;

            var dirInfo = new System.IO.DirectoryInfo(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory!));
            var iconPath = System.IO.Path.Combine(dirInfo.Parent!.FullName, "Images", "NotifyIcon.png");
            if (System.IO.File.Exists(iconPath))
            {
                icon = new System.Drawing.Bitmap(iconPath);
            }
            var iconDarkPath = System.IO.Path.Combine(dirInfo.Parent!.FullName, "Images", "NotifyIcon-Dark.png");
            if (System.IO.File.Exists(iconDarkPath))
            {
                iconDark = new System.Drawing.Bitmap(iconDarkPath);
            }

            themeChangedTimer = new DispatcherTimer(DispatcherPriority.Background);
            themeChangedTimer.Interval = TimeSpan.FromMilliseconds(50);
            themeChangedTimer.Tick += ThemeChangedTimer_Tick;

            UpdateNotifyIcon();
            notifyIcon.Visible = true;

            SystemEvents.UserPreferenceChanged += SystemEvents_UserPreferenceChanged;

            DispatcherHelper.UIDispatcher?.BeginInvoke(
                System.Windows.Threading.DispatcherPriority.Background,
                new Action(() =>
                {
                    UpdateToolTipText();
                    UpdateSettings();
                    ViewModelLocator.Instance.SettingsWindowViewModel.SettingsChanged += SettingsWindowViewModel_SettingsChanged;
                }));
        }

        private void SystemEvents_UserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e)
        {
            themeChangedTimer?.Stop();
            themeChangedTimer?.Start();
        }

        private void ThemeChangedTimer_Tick(object? sender, EventArgs e)
        {
            themeChangedTimer?.Stop();
            if (disposedValue) return;

            UpdateNotifyIcon();
        }

        private void UpdateNotifyIcon()
        {
            bool useDarkIcon = false; // 即任务栏为白色
            try
            {
                using (var baseKey = Microsoft.Win32.RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry64))
                using (var key = baseKey.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Themes\\Personalize"))
                {
                    if (key != null)
                    {
                        var isLight = key.GetValue("SystemUsesLightTheme");
                        if (isLight == null)
                        {
                            isLight = key.GetValue("AppsUseLightTheme");
                        }

                        useDarkIcon = Convert.ToInt32(isLight) == 1;
                    }
                }
            }
            catch { }

            notifyIcon.Icon = useDarkIcon ? iconDark : icon;
        }

        private async void ContextMenu_ItemClick(object sender, RoutedEventArgs e)
        {
            if (e.Source is System.Windows.Controls.MenuItem menuItem)
            {
                await Task.Delay(10);

                if (menuItem.Name == "ReadMe")
                {
                    ViewModelLocator.Instance.SettingsWindowViewModel.ShowReadMe();
                }
                else if (menuItem.Name == "Transparent")
                {
                    ViewModelLocator.Instance.SettingsWindowViewModel.WindowTransparent = menuItem.IsChecked;
                }
                else if (menuItem.Name == "AlwaysShowBackground")
                {
                    ViewModelLocator.Instance.SettingsWindowViewModel.AlwaysShowBackground = menuItem.IsChecked;
                }
                else if (menuItem.Name == "Karaoke")
                {
                    ViewModelLocator.Instance.SettingsWindowViewModel.KaraokeEnabled = menuItem.IsChecked;
                }
                else if (menuItem.Name == "Settings")
                {
                    ViewModelLocator.Instance.SettingsWindowViewModel.ShowSettingsWindow();
                }
                else if (menuItem.Name == "Exit")
                {
                    var lyricHostWindow = App.Current.Windows.OfType<HostWindow>().FirstOrDefault();
                    if (lyricHostWindow != null)
                    {
                        lyricHostWindow.SaveBounds();
                        lyricHostWindow.ApplicationExiting = true;
                        lyricHostWindow.Close();
                    }
                    Application.Current.Shutdown();
                }
                else if (menuItem.Parent is System.Windows.Controls.MenuItem parent)
                {
                    if (parent.Name == "HorizontalContentAlignment")
                    {
                        HorizontalAlignment horizontalAlignment = HorizontalAlignment.Left;

                        switch (menuItem.Name)
                        {
                            case "HorizontalContentAlignment_Left":
                                horizontalAlignment = HorizontalAlignment.Left;
                                break;

                            case "HorizontalContentAlignment_Center":
                                horizontalAlignment = HorizontalAlignment.Center;
                                break;

                            case "HorizontalContentAlignment_Right":
                                horizontalAlignment = HorizontalAlignment.Right;
                                break;
                        }

                        ViewModelLocator.Instance.SettingsWindowViewModel.LyricHorizontalAlignment = horizontalAlignment;
                    }
                }
            }
        }

        private void NotifyIcon_Click(object? sender, EventArgs e)
        {
            ViewModelLocator.Instance.SettingsWindowViewModel.ActivateInstance();
        }

        private void NotifyIcon_DoubleClick(object? sender, EventArgs e)
        {
            ViewModelLocator.Instance.SettingsWindowViewModel.WindowTransparent = false;
            ViewModelLocator.Instance.SettingsWindowViewModel.ActivateInstance();
        }

        private void SettingsWindowViewModel_SettingsChanged(object? sender, EventArgs e)
        {
            UpdateSettings();
        }

        public void UpdateToolTipText()
        {
            if (notifyIcon == null) return;

            var vm = ViewModelLocator.Instance.LyricWindowViewModel;
            var session = vm?.SelectedSession;
            var media = vm?.MediaModel;
            bool isEmptyMedia = session == null || media == null || media.IsEmptyLyric;

            var sb = new StringBuilder();
            sb.Append("热词");

            if (!isEmptyMedia)
            {
                sb.AppendLine();
                sb.Append(media!.DisplayText);
            }

            notifyIcon.ToolTipText = sb.ToString();
        }

        public void ToggleWindowTransparent()
        {
            var contextMenu = notifyIcon?.ContextMenu;
            if (contextMenu != null)
            {
                var item = contextMenu.Items.OfType<System.Windows.Controls.MenuItem>().FirstOrDefault(c => c.Name == "Transparent");
                var peer = (UIElementAutomationPeer.FromElement(item) as MenuItemAutomationPeer)
                    ?? (UIElementAutomationPeer.CreatePeerForElement(item) as MenuItemAutomationPeer);
                var invokeProvider = peer?.GetPattern(PatternInterface.Invoke) as IInvokeProvider;
                invokeProvider?.Invoke();
            }
        }

        private void UpdateSettings()
        {
            var settingsVm = ViewModelLocator.Instance.SettingsWindowViewModel;

            transparentMenuItem.IsChecked = settingsVm.WindowTransparent;
            alwaysShowBackgroundMenuItem.IsChecked = settingsVm.AlwaysShowBackground;
            karaokeMenuItem.IsChecked = settingsVm.KaraokeEnabled;

            var alignItems = lyricHorizontalAlignmentMenuItem.Items
                .OfType<System.Windows.Controls.MenuItem>()
                .Where(c => c.Name.StartsWith("HorizontalContentAlignment_"))
                .ToArray();

            foreach (var item in alignItems)
            {
                item.IsChecked = object.Equals(item.Tag, settingsVm.LyricHorizontalAlignment);
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: 释放托管状态(托管对象)
                    SystemEvents.UserPreferenceChanged -= SystemEvents_UserPreferenceChanged;
                    ViewModelLocator.Instance.SettingsWindowViewModel.PropertyChanged -= SettingsWindowViewModel_SettingsChanged;

                    if (themeChangedTimer != null)
                    {
                        themeChangedTimer.Stop();
                        themeChangedTimer.Tick -= ThemeChangedTimer_Tick;
                        themeChangedTimer = null!;
                    }

                    notifyIcon.Visible = false;
                    notifyIcon.Dispose();
                    notifyIcon = null!;

                    icon?.Dispose();
                    icon = null;

                    iconDark?.Dispose();
                    iconDark = null;
                }

                // TODO: 释放未托管的资源(未托管的对象)并重写终结器
                // TODO: 将大型字段设置为 null
                disposedValue = true;
            }
        }

        // // TODO: 仅当“Dispose(bool disposing)”拥有用于释放未托管资源的代码时才替代终结器
        // ~NotifyIconHelper()
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
