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
    /// LyricThemePreview.xaml 的交互逻辑
    /// </summary>
    public partial class LyricThemePreview : UserControl
    {
        public LyricThemePreview()
        {
            InitializeComponent();
        }

        public LyricThemeView Theme
        {
            get { return (LyricThemeView)GetValue(ThemeProperty); }
            set { SetValue(ThemeProperty, value); }
        }

        public static readonly DependencyProperty ThemeProperty =
            DependencyProperty.Register("Theme", typeof(LyricThemeView), typeof(LyricThemePreview), new PropertyMetadata(null, (s, a) =>
            {
                if (s is LyricThemePreview sender)
                {
                    sender.UpdateTheme();
                }
            }));


        private void UpdateTheme()
        {
            if (Theme == null) return;

            BackgroundBorder.Background = Theme.BackgroundBrush;
            BackgroundBorder.BorderBrush = Theme.BorderBrush;
            Text1.Fill = Theme.LyricBrush;
            Text2.Fill = Theme.KaraokeBrush;
            Text1.Stroke = Theme.LyricStrokeBrush;
            Text2.Stroke = Theme.KaraokeStrokeBrush;
        }

    }
}
