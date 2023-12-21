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
        private HotKeyModel?[] hotKeyModels;
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
                if (model != null)
                {
                    var settingValue = settingViewModel.LoadSetting($"{HotKeySettingKeyTemplate}{model.Id}", defaultHotKeys[model.Id]);

                    (model.Modifiers, model.VirtualKey) = HotKey.GetKeyFromSettingValue(settingValue);

                    model.RegisterPropertyChangedCallback(HotKeyModel.VirtualKeyProperty, OnHotKeyPropertyChanged);
                    model.RegisterPropertyChangedCallback(HotKeyModel.ModifiersProperty, OnHotKeyPropertyChanged);
                }
            }

            this.settingViewModel = settingViewModel;
        }

        public HotKeyModel? PlayPauseKeyModel { get; }

        public HotKeyModel? PrevMediaKeyModel { get; }

        public HotKeyModel? NextMediaKeyModel { get; }

        public HotKeyModel? VolumeUpKeyModel { get; }

        public HotKeyModel? VolumeDownKeyModel { get; }

        public HotKeyModel? ShowHideLyricKeyModel { get; }

        public HotKeyModel? LockUnlockKeyModel { get; }

        public HotKeyModel? OpenPlayerKeyModel { get; }


        private HotKeyModel? CreateHotKey(string name, string displayName)
        {
            var model = HotKeyManager.RegisterKey(name, 0, 0);

            if (model != null)
            {
                model.Label = displayName;
            }

            return model;
        }

        private void OnHotKeyPropertyChanged(DependencyObject sender, DependencyProperty dp)
        {
            if (sender is HotKeyModel hotKeyModel)
            {
                var model = hotKeyModels.FirstOrDefault(c => c == hotKeyModel);

                if (model != null)
                {
                    settingViewModel.SetSettings($"{HotKeySettingKeyTemplate}{model.Id}", HotKey.ToSettingValue(model));
                }
            }
        }

        public void ResetToDefaultSettings()
        {
            foreach (var model in hotKeyModels)
            {
                if (model != null)
                {
                    (model.Modifiers, model.VirtualKey) =
                        HotKey.GetKeyFromSettingValue(defaultHotKeys[model.Id]);
                }
            }
        }
    }

    public static class HotKey
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ToSettingValue(HotKeyModel hotKeyModel)
        {
            return BuildSettingValue(hotKeyModel.Modifiers, hotKeyModel.VirtualKey);
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
