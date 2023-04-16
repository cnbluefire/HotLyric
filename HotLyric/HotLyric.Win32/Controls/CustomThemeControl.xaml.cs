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
                        sender.WindowBorderColorPicker.SelectedColor = GetBrushColor(newTheme.BorderBrush);
                        sender.WindowBackgroundColorPicker.SelectedColor = GetBrushColor(newTheme.BackgroundBrush);
                        sender.LyricTextColorPicker.SelectedColor = GetBrushColor(newTheme.LyricBrush);
                        sender.KaraokeTextColorPicker.SelectedColor = GetBrushColor(newTheme.KaraokeBrush);
                        sender.LyricStrokeColorPicker.SelectedColor = GetBrushColor(newTheme.LyricStrokeBrush);
                        sender.KaraokeStrokeColorPicker.SelectedColor = GetBrushColor(newTheme.KaraokeStrokeBrush);
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
                var windowBorderColor = GetBrushColor(Theme.BorderBrush);
                var windowBackgroundColor = GetBrushColor(Theme.BackgroundBrush);
                var lyricTextColor = GetBrushColor(Theme.LyricBrush);
                var karaokeTextColor = GetBrushColor(Theme.KaraokeBrush);
                var lyricStrokeColor = GetBrushColor(Theme.LyricStrokeBrush);
                var karaokeStrokeColor = GetBrushColor(Theme.KaraokeStrokeBrush);

                if((windowBorderColor, windowBackgroundColor, lyricTextColor, karaokeTextColor, lyricStrokeColor, karaokeStrokeColor) ==
                    (WindowBorderColorPicker.SelectedColor, WindowBackgroundColorPicker.SelectedColor, LyricTextColorPicker.SelectedColor, KaraokeTextColorPicker.SelectedColor, LyricStrokeColorPicker.SelectedColor, KaraokeStrokeColorPicker.SelectedColor))
                {
                    return;
                }
            }

            var theme = new LyricThemeView(
                new SolidColorBrush(WindowBorderColorPicker.SelectedColor),
                new SolidColorBrush(WindowBackgroundColorPicker.SelectedColor),
                new SolidColorBrush(LyricTextColorPicker.SelectedColor),
                new SolidColorBrush(KaraokeTextColorPicker.SelectedColor),
                new SolidColorBrush(LyricStrokeColorPicker.SelectedColor),
                new SolidColorBrush(KaraokeStrokeColorPicker.SelectedColor));

            Theme = theme;
        }

        private static Color GetBrushColor(Brush? brush)
        {
            if (brush is SolidColorBrush scb) return scb.Color;
            return Color.FromArgb(255, 255, 255, 255);
        }

    }
}
