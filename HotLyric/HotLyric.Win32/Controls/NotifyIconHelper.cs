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
using H.NotifyIcon;
using Microsoft.UI.Xaml.Controls;
using CommunityToolkit.Mvvm.Input;
using HotLyric.Win32.Controls.LyricControlDrawingData;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media.Imaging;
using Vanara.PInvoke;
using Windows.UI.ViewManagement;
using System.Runtime;
using BlueFire.Toolkit.WinUI3.Input;

namespace HotLyric.Win32.Controls
{
    public class NotifyIconHelper : IDisposable
    {
        private bool disposedValue;
        private TaskbarIcon notifyIcon;

        private MenuFlyoutItem launcherMenuItem;
        private ToggleMenuFlyoutItem transparentMenuItem;
        private ToggleMenuFlyoutItem alwaysShowBackgroundMenuItem;
        private ToggleMenuFlyoutItem karaokeMenuItem;
        private MenuFlyoutSubItem lyricHorizontalAlignmentMenuItem;
        private MenuFlyoutItem settingsMenuItem;
        private MenuFlyoutItem exitMenuItem;

        private DispatcherTimer themeChangedTimer;
        private UISettings uiSettings;

        public NotifyIconHelper()
        {
            notifyIcon = new TaskbarIcon();

            var contextMenu = new MenuFlyout();
            var command = new AsyncRelayCommand<MenuFlyoutItemBase>(OnMenuItemClick);
            var toggleCommand = new AsyncRelayCommand<ToggleMenuFlyoutItem>(async item =>
            {
                if (item != null)
                {
                    item.IsChecked = !item.IsChecked;
                }
                await OnMenuItemClick(item);
            });

            launcherMenuItem = Extensions.CreateMenuItem<MenuFlyoutItem>("使用帮助", "ReadMe", command);
            transparentMenuItem = Extensions.CreateMenuItem<ToggleMenuFlyoutItem>("锁定歌词", "Transparent", toggleCommand);
            alwaysShowBackgroundMenuItem = Extensions.CreateMenuItem<ToggleMenuFlyoutItem>("始终显示背景", "AlwaysShowBackground", toggleCommand);
            karaokeMenuItem = Extensions.CreateMenuItem<ToggleMenuFlyoutItem>("卡拉OK模式", "Karaoke", toggleCommand, null, item =>
            {
                item.IsChecked = true;
            });

            lyricHorizontalAlignmentMenuItem = Extensions.CreateMenuItem<MenuFlyoutSubItem>("歌词对齐方式", "LyricAlignment", command, null, item =>
            {
                item.Items.Add(
                    Extensions.CreateMenuItem<ToggleMenuFlyoutItem>("左对齐", "LyricAlignment_Left", command, null, subItem =>
                    {
                        subItem.Tag = LyricDrawingLineAlignment.Left;
                    }));

                item.Items.Add(
                    Extensions.CreateMenuItem<ToggleMenuFlyoutItem>("居中", "LyricAlignment_Center", command, null, subItem =>
                    {
                        subItem.Tag = LyricDrawingLineAlignment.Center;
                    }));

                item.Items.Add(
                    Extensions.CreateMenuItem<ToggleMenuFlyoutItem>("右对齐", "LyricAlignment_Right", command, null, subItem =>
                    {
                        subItem.Tag = LyricDrawingLineAlignment.Right;
                    }));
            });

            settingsMenuItem = Extensions.CreateMenuItem<MenuFlyoutItem>("设置", "Settings", command);
            exitMenuItem = Extensions.CreateMenuItem<MenuFlyoutItem>("退出", "Exit", command);

            contextMenu.Items.Add(launcherMenuItem);
            contextMenu.Items.Add(transparentMenuItem);
            contextMenu.Items.Add(alwaysShowBackgroundMenuItem);
            contextMenu.Items.Add(karaokeMenuItem);
            contextMenu.Items.Add(lyricHorizontalAlignmentMenuItem);

            contextMenu.Items.Add(new MenuFlyoutSeparator());
            contextMenu.Items.Add(settingsMenuItem);

            contextMenu.Items.Add(new MenuFlyoutSeparator());
            contextMenu.Items.Add(exitMenuItem);

            notifyIcon.ContextFlyout = contextMenu;

            notifyIcon.LeftClickCommand = new RelayCommand(NotifyIcon_Click);
            notifyIcon.DoubleClickCommand = new RelayCommand(NotifyIcon_DoubleClick);
            notifyIcon.RightClickCommand = new RelayCommand(NotifyIcon_RightClick);

            themeChangedTimer = new DispatcherTimer()
            {
                Interval = TimeSpan.FromMilliseconds(100),
            };
            themeChangedTimer.Tick += ThemeChangedTimer_Tick;

            notifyIcon.ForceCreate(false);
            _ = UpdateNotifyIconAsync();
            notifyIcon.Visibility = Visibility.Visible;

            uiSettings = new UISettings();
            uiSettings.ColorValuesChanged += UISettings_ColorValuesChanged;

            App.DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, () =>
            {
                UpdateToolTipText();
                UpdateSettings();
                ViewModelLocator.Instance.SettingsWindowViewModel.SettingsChanged += SettingsWindowViewModel_SettingsChanged;
            });
        }

        private void NotifyIcon_RightClick()
        {
            transparentMenuItem.Text = GetMenuItemText(
                "锁定歌词",
                ViewModelLocator.Instance.SettingsWindowViewModel.HotKeyModels.LockUnlockKeyModel);

            static string GetMenuItemText(string _text, HotKeyModel? _hotKey)
            {
                if (_hotKey != null && _hotKey.IsEnabled)
                {
                    var _hotKeyText = _hotKey.ToString(true);

                    if (!string.IsNullOrEmpty(_hotKeyText)) return $"{_text} [{_hotKeyText}]";
                }

                return _text;
            }
        }

        private async void ThemeChangedTimer_Tick(object? sender, object e)
        {
            themeChangedTimer?.Stop();
            if (disposedValue) return;

            await UpdateNotifyIconAsync();
        }

        private void UISettings_ColorValuesChanged(UISettings sender, object args)
        {
            App.DispatcherQueue.TryEnqueue(() =>
            {
                themeChangedTimer?.Stop();
                themeChangedTimer?.Start();
            });
        }

        private async Task UpdateNotifyIconAsync()
        {
            try
            {
                notifyIcon.Icon = await IconHelper.CreateIconAsync(
                    GetCurrentThemeIconUri(),
                    IconHelper.GetSmallIconSize(),
                    IconHelper.GetPrimaryDisplayDpiScale(),
                    default);
            }
            catch (Exception ex)
            {
                HotLyric.Win32.Utils.LogHelper.LogError(ex);
            }
        }

        private async Task OnMenuItemClick(MenuFlyoutItemBase? menuItem)
        {
            if (menuItem == null) return;

            await Task.Delay(10);

            if (menuItem.Name == "ReadMe")
            {
                ViewModelLocator.Instance.SettingsWindowViewModel.ShowReadMe();
            }
            else if (menuItem.Name == "Transparent")
            {
                ViewModelLocator.Instance.SettingsWindowViewModel.WindowTransparent = menuItem.IsChecked();
                ViewModelLocator.Instance.SettingsWindowViewModel.ActivateInstance();
            }
            else if (menuItem.Name == "AlwaysShowBackground")
            {
                ViewModelLocator.Instance.SettingsWindowViewModel.AlwaysShowBackground = menuItem.IsChecked();
            }
            else if (menuItem.Name == "Karaoke")
            {
                ViewModelLocator.Instance.SettingsWindowViewModel.KaraokeEnabled = menuItem.IsChecked();
            }
            else if (menuItem.Name == "Settings")
            {
                ViewModelLocator.Instance.SettingsWindowViewModel.ShowSettingsWindow();
            }
            else if (menuItem.Name == "Exit")
            {
                await Task.Delay(500);

                var lyricHostWindow = App.Current.LyricView;
                if (lyricHostWindow != null)
                {
                    lyricHostWindow.XamlWindow.Close();
                }
                App.Current.Exit();
            }
            else if (menuItem.Name.StartsWith("LyricAlignment_"))
            {
                LyricDrawingLineAlignment alignment = (LyricDrawingLineAlignment)menuItem.Tag;

                ViewModelLocator.Instance.SettingsWindowViewModel.LyricAlignments.SelectedValue = alignment;

                UpdateSelectedAlignmentItem();
            }
        }

        private void UpdateSelectedAlignmentItem()
        {
            var alignment = (object)(ViewModelLocator.Instance.SettingsWindowViewModel.LyricAlignments.SelectedValue ?? LyricDrawingLineAlignment.Left);
            foreach (var item in lyricHorizontalAlignmentMenuItem.Items.OfType<ToggleMenuFlyoutItem>())
            {
                item.IsChecked = Equals(item.Tag, alignment);
            }
        }

        private void NotifyIcon_Click()
        {
            ViewModelLocator.Instance.SettingsWindowViewModel.ActivateInstance();
        }

        private void NotifyIcon_DoubleClick()
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
            transparentMenuItem.IsChecked = !transparentMenuItem.IsChecked;
            _ = OnMenuItemClick(transparentMenuItem);
        }

        private void UpdateSettings()
        {
            var settingsVm = ViewModelLocator.Instance.SettingsWindowViewModel;

            transparentMenuItem.IsChecked = settingsVm.WindowTransparent;
            alwaysShowBackgroundMenuItem.IsChecked = settingsVm.AlwaysShowBackground;
            karaokeMenuItem.IsChecked = settingsVm.KaraokeEnabled;

            UpdateSelectedAlignmentItem();
        }

        private static Uri GetCurrentThemeIconUri()
        {
            const string lightIconUri = "ms-appx:///Images/NotifyIcon.png";
            const string darkIconUri = "ms-appx:///Images/NotifyIcon-Dark.png";

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
            catch (Exception ex)
            {
                HotLyric.Win32.Utils.LogHelper.LogError(ex);
            }

            return new Uri((useDarkIcon ? darkIconUri : lightIconUri));
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: 释放托管状态(托管对象)
                    uiSettings.ColorValuesChanged -= UISettings_ColorValuesChanged;
                    uiSettings = null!;
                    ViewModelLocator.Instance.SettingsWindowViewModel.PropertyChanged -= SettingsWindowViewModel_SettingsChanged;

                    if (themeChangedTimer != null)
                    {
                        themeChangedTimer.Stop();
                        themeChangedTimer.Tick -= ThemeChangedTimer_Tick;
                        themeChangedTimer = null!;
                    }

                    notifyIcon.Visibility = Visibility.Collapsed;
                    notifyIcon.Dispose();
                    notifyIcon = null!;
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

    file static class Extensions
    {
        public static T SetupCommand<T>(this T item, System.Windows.Input.ICommand? command, object? commandParams) where T : MenuFlyoutItemBase, new()
            => SetupCommand(item, null, null, command, commandParams);

        public static T SetupCommand<T>(this T item, string? text, string? name, System.Windows.Input.ICommand? command, object? commandParams) where T : MenuFlyoutItemBase, new()
        {
            if (!string.IsNullOrEmpty(name))
                item.Name = name;

            {
                if (item is MenuFlyoutItem menuItem)
                {
                    if (!string.IsNullOrEmpty(text))
                        menuItem.Text = text;

                    menuItem.Command = command;
                    menuItem.CommandParameter = commandParams ?? item;
                }
            }
            {
                if (item is ToggleMenuFlyoutItem menuItem)
                {
                    if (!string.IsNullOrEmpty(text))
                        menuItem.Text = text;

                    menuItem.Command = command;
                    menuItem.CommandParameter = commandParams ?? item;
                }
            }
            {
                if (item is RadioMenuFlyoutItem menuItem)
                {
                    if (!string.IsNullOrEmpty(text))
                        menuItem.Text = text;

                    menuItem.Command = command;
                    menuItem.CommandParameter = commandParams ?? item;
                }
            }
            {
                if (item is MenuFlyoutSubItem menuItem)
                {
                    if (!string.IsNullOrEmpty(text))
                        menuItem.Text = text;
                }
            }

            return item;
        }

        public static T CreateMenuItem<T>(string text, string name, System.Windows.Input.ICommand? command, object? commandParams = null, Action<T>? options = null) where T : MenuFlyoutItemBase, new()
        {
            var item = new T();

            item.SetupCommand(text, name, command, commandParams);

            options?.Invoke(item);

            return item;
        }

        public static bool IsChecked(this MenuFlyoutItemBase item)
        {
            { if (item is ToggleMenuFlyoutItem menuItem) return menuItem.IsChecked; }
            { if (item is RadioMenuFlyoutItem menuItem) return menuItem.IsChecked; }

            return false;
        }


        public static void IsChecked(this MenuFlyoutItemBase item, bool value)
        {
            { if (item is ToggleMenuFlyoutItem menuItem) menuItem.IsChecked = value; }
            { if (item is RadioMenuFlyoutItem menuItem) menuItem.IsChecked = value; }
        }
    }
}
