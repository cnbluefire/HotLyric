using BlueFire.Toolkit.WinUI3.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using HotLyric.Win32.Utils;
using HotLyric.Win32.ViewModels;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Vanara.PInvoke;

namespace HotLyric.Win32.Models
{
    public class HotKeyModels : ObservableObject
    {
        private const string HotKeySettingKeyTemplate = "Settings_HotKey_";

        private readonly SettingsWindowViewModel settingViewModel;
        private HotKey[] hotKeyModels;
        private Dictionary<string, int> defaultHotKeys = new Dictionary<string, int>()
        {
            ["PlayPause"] = HotKey.BuildSettingValue(HotKeyModifiers.MOD_CONTROL | HotKeyModifiers.MOD_ALT, VirtualKeys.VK_P),
            ["PrevMedia"] = HotKey.BuildSettingValue(HotKeyModifiers.MOD_CONTROL | HotKeyModifiers.MOD_ALT, VirtualKeys.VK_LEFT),
            ["NextMedia"] = HotKey.BuildSettingValue(HotKeyModifiers.MOD_CONTROL | HotKeyModifiers.MOD_ALT, VirtualKeys.VK_RIGHT),
            ["VolumeUp"] = HotKey.BuildSettingValue(HotKeyModifiers.MOD_CONTROL | HotKeyModifiers.MOD_ALT, VirtualKeys.VK_UP),
            ["VolumeDown"] = HotKey.BuildSettingValue(HotKeyModifiers.MOD_CONTROL | HotKeyModifiers.MOD_ALT, VirtualKeys.VK_DOWN),
            ["ShowHideLyric"] = HotKey.BuildSettingValue(HotKeyModifiers.MOD_CONTROL | HotKeyModifiers.MOD_ALT, VirtualKeys.VK_D),
            ["LockUnlock"] = HotKey.BuildSettingValue(HotKeyModifiers.MOD_CONTROL | HotKeyModifiers.MOD_ALT, VirtualKeys.VK_E),
            ["OpenPlayer"] = HotKey.BuildSettingValue(HotKeyModifiers.MOD_CONTROL | HotKeyModifiers.MOD_ALT, VirtualKeys.VK_H),
        };

        public HotKeyModels(SettingsWindowViewModel settingViewModel)
        {
            PlayPauseKeyModel = CreateHotKey("PlayPause", "播放/暂停");
            PrevMediaKeyModel = CreateHotKey("PrevMedia", "上一曲");
            NextMediaKeyModel = CreateHotKey("NextMedia", "下一曲");
            VolumeUpKeyModel = CreateHotKey("VolumeUp", "加大音量");
            VolumeDownKeyModel = CreateHotKey("VolumeDown", "减小音量");
            ShowHideLyricKeyModel = CreateHotKey("ShowHideLyric", "显示/隐藏歌词");
            LockUnlockKeyModel = CreateHotKey("LockUnlock", "锁定/解锁歌词");
            OpenPlayerKeyModel = CreateHotKey("OpenPlayer", "显示播放器");

            hotKeyModels = new[]
            {
                PlayPauseKeyModel,
                PrevMediaKeyModel,
                NextMediaKeyModel,
                VolumeUpKeyModel,
                VolumeDownKeyModel,
                ShowHideLyricKeyModel,
                LockUnlockKeyModel,
                OpenPlayerKeyModel,
            };

            foreach (var model in hotKeyModels)
            {
                var settingValue = settingViewModel.LoadSetting($"{HotKeySettingKeyTemplate}{model.HotKeyName}", defaultHotKeys[model.HotKeyName]);

                (model.HotKeyModel.Modifiers, model.HotKeyModel.VirtualKey) = HotKey.GetKeyFromSettingValue(settingValue);

                model.HotKeyModel.RegisterPropertyChangedCallback(HotKeyModel.VirtualKeyProperty, OnHotKeyPropertyChanged);
                model.HotKeyModel.RegisterPropertyChangedCallback(HotKeyModel.ModifiersProperty, OnHotKeyPropertyChanged);
            }

            this.settingViewModel = settingViewModel;

            HotKeyManager.HotKeyInvoked += HotKeyManager_HotKeyInvoked;
        }

        public HotKey PlayPauseKeyModel { get; }

        public HotKey PrevMediaKeyModel { get; }

        public HotKey NextMediaKeyModel { get; }

        public HotKey VolumeUpKeyModel { get; }

        public HotKey VolumeDownKeyModel { get; }

        public HotKey ShowHideLyricKeyModel { get; }

        public HotKey LockUnlockKeyModel { get; }

        public HotKey OpenPlayerKeyModel { get; }


        private HotKey CreateHotKey(string name, string displayName)
        {
            var model = new HotKeyModel();
            HotKeyManager.RegisterKey(model);

            return new HotKey(name, displayName, model);
        }

        private void OnHotKeyPropertyChanged(DependencyObject sender, DependencyProperty dp)
        {
            if (sender is HotKeyModel hotKeyModel)
            {
                var model = hotKeyModels.FirstOrDefault(c => c.HotKeyModel == hotKeyModel);

                if (model != null)
                {
                    settingViewModel.SetSettings($"{HotKeySettingKeyTemplate}{model.HotKeyName}", model.ToSettingValue());
                }
            }
        }

        private void HotKeyManager_HotKeyInvoked(object? sender, HotKeyInvokedEventArgs args)
        {
            var model = hotKeyModels.FirstOrDefault(c => c.HotKeyModel.VirtualKey == args.Key && c.HotKeyModel.Modifiers == args.Modifier);
            if (model != null)
            {
                HotKeyInvoked?.Invoke(this, new HotKeyManagerHotKeyInvokedEventArgs(model));
            }
        }

        public event HotKeyManagerHotKeyInvokedEventHandler? HotKeyInvoked;

        public void ResetToDefaultSettings()
        {
            foreach (var model in hotKeyModels)
            {
                (model.HotKeyModel.Modifiers, model.HotKeyModel.VirtualKey) =
                    HotKey.GetKeyFromSettingValue(defaultHotKeys[model.HotKeyName]);
            }
        }
    }

    public delegate void HotKeyManagerHotKeyInvokedEventHandler(HotKeyModels sender, HotKeyManagerHotKeyInvokedEventArgs args);

    public record HotKeyManagerHotKeyInvokedEventArgs(HotKey HotKeyModel);

    public record HotKey(string HotKeyName, string DisplayName, HotKeyModel HotKeyModel)
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int ToSettingValue()
        {
            return BuildSettingValue(HotKeyModel.Modifiers, HotKeyModel.VirtualKey);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static HotKey CreateFromSettingValue(string hotKeyName, string displayName, int settingValue)
        {
            var (modifiers, key) = GetKeyFromSettingValue(settingValue);

            return new HotKey(hotKeyName, displayName, new HotKeyModel()
            {
                VirtualKey = key,
                Modifiers = modifiers
            });
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int BuildSettingValue(HotKeyModifiers modifiers, VirtualKeys key)
        {
            return (int)modifiers << 16 | (int)key;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static (HotKeyModifiers modifiers, VirtualKeys key) GetKeyFromSettingValue(int settingValue)
        {
            var modifiers = (HotKeyModifiers)(settingValue >> 16);
            var key = (VirtualKeys)(settingValue & 0xFFFF);

            return (modifiers, key);
        }

        public static Visibility IsRegisterFailed(HotKeyModelStatus status) => status switch
        {
            HotKeyModelStatus.RegisterFailed => Visibility.Visible,
            _ => Visibility.Collapsed
        };
    }
}
