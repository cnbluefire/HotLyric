using HotLyric.Win32.Controls.LyricControlDrawingData;
using HotLyric.Win32.Models;
using HotLyric.Win32.Utils.LyricFiles;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Windows.UI;

//https://go.microsoft.com/fwlink/?LinkId=234236 上介绍了“用户控件”项模板

namespace HotLyric.Win32.Controls
{
    public sealed partial class TextThemePreviewControl : UserControl
    {
        private LyricDrawingLine? line;
        private LyricDrawingTextColors colors;

        public TextThemePreviewControl()
        {
            this.InitializeComponent();
            this.Loaded += TextThemePreviewControl_Loaded;
            this.Unloaded += TextThemePreviewControl_Unloaded;

            colors = new LyricDrawingTextColors();
        }

        private void TextThemePreviewControl_Loaded(object sender, RoutedEventArgs e)
        {
            CanvasControl.Draw += CanvasControl_Draw;
            CanvasControl.Invalidate();
        }

        private void TextThemePreviewControl_Unloaded(object sender, RoutedEventArgs e)
        {
            CanvasControl.Draw -= CanvasControl_Draw;
        }

        private void CanvasControl_Draw(Microsoft.Graphics.Canvas.UI.Xaml.CanvasControl sender, Microsoft.Graphics.Canvas.UI.Xaml.CanvasDrawEventArgs args)
        {
            try
            {
                if (line == null)
                {
                    line = new LyricDrawingLine(
                        args.DrawingSession,
                        sender.Size,
                        new SampleLine("字"),
                        string.IsNullOrEmpty(FontFamily?.Source) ? "Microsoft Yahei UI" : FontFamily?.Source!,
                        LyricDrawingLineType.Classic,
                        LyricDrawingLineAlignment.Center,
                        1,
                        LyricDrawingLineTextSizeType.DrawSize);
                }

                line.Draw(args.DrawingSession, new LyricDrawingParameters(0.5, 1, true, LyricControlProgressAnimationMode.Karaoke, colors));
            }
            catch (Exception ex) when (sender.Device.IsDeviceLost(ex.HResult))
            {
                sender.Device.RaiseDeviceLost();
            }
        }


        public LyricThemeView Theme
        {
            get { return (LyricThemeView)GetValue(ThemeProperty); }
            set { SetValue(ThemeProperty, value); }
        }

        public static readonly DependencyProperty ThemeProperty =
            DependencyProperty.Register("Theme", typeof(LyricThemeView), typeof(TextThemePreviewControl), new PropertyMetadata(null, (s, a) =>
            {
                if (s is TextThemePreviewControl sender)
                {
                    sender.UpdateTheme();
                }
            }));

        private void UpdateTheme()
        {
            if (Theme == null) return;

            BackgroundBorder.Background = Theme.BackgroundBrush;
            BackgroundBorder.BorderBrush = Theme.BorderBrush;

            colors.FillColor1 = (Theme.LyricBrush as SolidColorBrush)?.Color ?? Color.FromArgb(0, 0, 0, 0);
            colors.FillColor2 = (Theme.KaraokeBrush as SolidColorBrush)?.Color ?? Color.FromArgb(0, 0, 0, 0);
            colors.StrokeColor1 = (Theme.LyricStrokeBrush as SolidColorBrush)?.Color ?? Color.FromArgb(0, 0, 0, 0);
            colors.StrokeColor2 = (Theme.KaraokeStrokeBrush as SolidColorBrush)?.Color ?? Color.FromArgb(0, 0, 0, 0);

            if (IsLoaded)
            {
                CanvasControl.Invalidate();
            }
        }

        private class SampleLine : ILyricLine
        {
            public SampleLine(string text)
            {
                Text = text;
            }

            public TimeSpan StartTime => TimeSpan.FromSeconds(0);

            public TimeSpan EndTime => TimeSpan.FromSeconds(1);

            public bool IsEndLine => false;

            public string Text { get; }

            public IReadOnlyList<ILyricLineSpan> AllSpans => throw new NotImplementedException();

            public ILyricLineSpan? GetCurrentOrNextSpan(TimeSpan time)
            {
                throw new NotImplementedException();
            }

            public ILyricLineSpan? GetCurrentSpan(TimeSpan time)
            {
                throw new NotImplementedException();
            }

            public ILyricLineSpan? GetNextSpan(TimeSpan time)
            {
                throw new NotImplementedException();
            }
        }
    }
}
