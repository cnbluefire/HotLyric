using HotLyric.Win32.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace HotLyric.Win32.Controls
{
    /// <summary>
    /// CustomizeLyricThemeControl.xaml 的交互逻辑
    /// </summary>
    public partial class CustomizeLyricThemeControl : UserControl
    {
        public CustomizeLyricThemeControl()
        {
            InitializeComponent();

            BorderColorButton.ColorChanged += OnColorChanged;
            BackgroundColorButton.ColorChanged += OnColorChanged;
            LyricColorButton.ColorChanged += OnColorChanged;
            KaraokeColorButton.ColorChanged += OnColorChanged;
            LyricStrokeColorButton.ColorChanged += OnColorChanged;
            KaraokeStrokeColorButton.ColorChanged += OnColorChanged;
        }

        private bool innerSet;

        public LyricThemeView Theme
        {
            get { return (LyricThemeView)GetValue(ThemeProperty); }
            set { SetValue(ThemeProperty, value); }
        }

        public static readonly DependencyProperty ThemeProperty =
            DependencyProperty.Register("Theme", typeof(LyricThemeView), typeof(CustomizeLyricThemeControl), new PropertyMetadata(null, (s, a) =>
            {
                if (!object.Equals(a.NewValue, a.OldValue) && s is CustomizeLyricThemeControl sender)
                {
                    sender.OnThemeChanged();
                }
            }));


        private void OnColorChanged(object? sender, EventArgs e)
        {
            if (innerSet) return;
            Theme = new LyricThemeView(
                new SolidColorBrush(BorderColorButton.Color),
                new SolidColorBrush(BackgroundColorButton.Color),
                new SolidColorBrush(LyricColorButton.Color),
                new SolidColorBrush(KaraokeColorButton.Color),
                new SolidColorBrush(LyricStrokeColorButton.Color),
                new SolidColorBrush(KaraokeStrokeColorButton.Color));
        }

        private void OnThemeChanged()
        {
            innerSet = true;

            if(Theme.BorderBrush is SolidColorBrush borderBrush) BorderColorButton.Color = borderBrush.Color;
            if(Theme.BackgroundBrush is SolidColorBrush backgroundBrush) BackgroundColorButton.Color = backgroundBrush.Color;
            if(Theme.LyricBrush is SolidColorBrush lyricBrush) LyricColorButton.Color = lyricBrush.Color;
            if(Theme.KaraokeBrush is SolidColorBrush karaokeBrush) KaraokeColorButton.Color = karaokeBrush.Color;
            if(Theme.LyricStrokeBrush is SolidColorBrush lyricStrokeBrush) LyricStrokeColorButton.Color = lyricStrokeBrush.Color;
            if(Theme.KaraokeStrokeBrush is SolidColorBrush karaokeStrokeBrush) KaraokeStrokeColorButton.Color = karaokeStrokeBrush.Color;

            innerSet = false;

            ThemeChanged?.Invoke(this, EventArgs.Empty);
        }

        public event EventHandler? ThemeChanged;
    }
}
