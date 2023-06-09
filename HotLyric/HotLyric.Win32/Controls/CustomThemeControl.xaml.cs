using HotLyric.Win32.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace HotLyric.Win32.Controls
{
    public sealed partial class CustomThemeControl : UserControl
    {
        public CustomThemeControl()
        {
            this.InitializeComponent();
            dispatcherTimer = new DispatcherTimer()
            {
                Interval = TimeSpan.FromSeconds(0.1)
            };
            dispatcherTimer.Tick += DispatcherTimer_Tick;
        }

        private DispatcherTimer dispatcherTimer;

        public LyricThemeView Theme
        {
            get { return (LyricThemeView)GetValue(ThemeProperty); }
            set { SetValue(ThemeProperty, value); }
        }

        public static readonly DependencyProperty ThemeProperty =
            DependencyProperty.Register("Theme", typeof(LyricThemeView), typeof(CustomThemeControl), new PropertyMetadata(null, (s, a) =>
            {
                if (s is CustomThemeControl sender && !Equals(a.NewValue, a.OldValue))
                {
                    if (a.NewValue is LyricThemeView newTheme)
                    {
                        sender.WindowBorderColorPicker.SelectedColor = newTheme.BorderColor;
                        sender.WindowBackgroundColorPicker.SelectedColor = newTheme.BackgroundColor;
                        sender.LyricTextColorPicker.SelectedColor = newTheme.LyricColor;
                        sender.KaraokeTextColorPicker.SelectedColor = newTheme.KaraokeColor;
                        sender.LyricStrokeColorPicker.SelectedColor = newTheme.LyricStrokeColor;
                        sender.KaraokeStrokeColorPicker.SelectedColor = newTheme.KaraokeStrokeColor;
                    }
                }
            }));

        private void ColorPicker_SelectedColorChanged(object sender, EventArgs e)
        {
            dispatcherTimer.Start();
        }

        private void DispatcherTimer_Tick(object? sender, object e)
        {
            dispatcherTimer.Stop();

            if (Theme != null)
            {
                var windowBorderColor = Theme.BorderColor;
                var windowBackgroundColor = Theme.BackgroundColor;
                var lyricTextColor = Theme.LyricColor;
                var karaokeTextColor = Theme.KaraokeColor;
                var lyricStrokeColor = Theme.LyricStrokeColor;
                var karaokeStrokeColor = Theme.KaraokeStrokeColor;

                if ((windowBorderColor, windowBackgroundColor, lyricTextColor, karaokeTextColor, lyricStrokeColor, karaokeStrokeColor) ==
                    (WindowBorderColorPicker.SelectedColor, WindowBackgroundColorPicker.SelectedColor, LyricTextColorPicker.SelectedColor, KaraokeTextColorPicker.SelectedColor, LyricStrokeColorPicker.SelectedColor, KaraokeStrokeColorPicker.SelectedColor))
                {
                    return;
                }
            }

            var theme = new LyricThemeView(
                WindowBorderColorPicker.SelectedColor,
                WindowBackgroundColorPicker.SelectedColor,
                LyricTextColorPicker.SelectedColor,
                KaraokeTextColorPicker.SelectedColor,
                LyricStrokeColorPicker.SelectedColor,
                KaraokeStrokeColorPicker.SelectedColor);

            Theme = theme;
        }
    }
}
