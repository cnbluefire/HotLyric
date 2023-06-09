using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using Microsoft.UI;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace HotLyric.Win32.Models
{
    public class LyricThemeView
    {
        public LyricThemeView(Color borderColor, Color backgroundColor, Color lyricColor, Color karaokeColor, Color lyricStrokeColor, Color karaokeStrokeColor)
            : this("customize", borderColor, backgroundColor, lyricColor, karaokeColor, lyricStrokeColor, karaokeStrokeColor) { }

        internal LyricThemeView(string name, Color borderColor, Color backgroundColor, Color lyricColor, Color karaokeColor, Color lyricStrokeColor, Color karaokeStrokeColor)
        {
            Name = name;

            BorderColor = borderColor;
            BackgroundColor = backgroundColor;
            LyricColor = lyricColor;
            KaraokeColor = karaokeColor;
            LyricStrokeColor = lyricStrokeColor;
            KaraokeStrokeColor = karaokeStrokeColor;

            if (KaraokeStrokeColor.A == 0)
            {
                KaraokeStrokeColor = LyricStrokeColor;
            }

            var color = BackgroundColor;

            var l = (color.R * 0.2126f + color.G * 0.7152f + color.B * 0.0722f) / 255f;

            ForegroundColor = l >= 0.65 ? Color.FromArgb(230, 0, 0, 0) : Color.FromArgb(204, 255, 255, 255);
            ForegroundBrush = new SolidColorBrush(ForegroundColor);
        }

        public string Name { get; }

        public Color BorderColor { get; }

        public Color BackgroundColor { get; }

        public Color LyricColor { get; }

        public Color KaraokeColor { get; }

        public Color LyricStrokeColor { get; }

        public Color KaraokeStrokeColor { get; }

        public Color ForegroundColor { get; }

        public Brush? ForegroundBrush { get; }
    }
}
