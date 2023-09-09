using BlueFire.Toolkit.WinUI3.Text;
using HotLyric.Win32.Models;
using HotLyric.Win32.Utils;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Text;
using Microsoft.UI.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI.Composition;
using Windows.UI.Text;

namespace HotLyric.Win32.Controls.LyricControlDrawingData
{
    internal class LyricDrawingTextGlyphRunGroup : IDisposable
    {
        private bool disposedValue;
        private IReadOnlyList<LyricDrawingTextGlyphRun> glyphRuns;
        private readonly Size textLayoutSize;
        private readonly Size textDrawSize;
        private readonly FontWeight fontWeight;
        private readonly FontStyle fontStyle;

        private LyricDrawingTextGlyphRunGroup(ICanvasResourceCreator resourceCreator, string textString, IReadOnlyList<CanvasFontFamily> fontFamilies, FontWeight fontWeight, FontStyle fontStyle)
        {
            this.fontWeight = fontWeight;
            this.fontStyle = fontStyle;

            var renderImpl = new LyricTextLineRendererImpl(resourceCreator, textString, fontFamilies, fontWeight, fontStyle);

            glyphRuns = renderImpl.GlyphRuns;
            PrimaryFontFamily = renderImpl.PrimaryFontFamily;
            textLayoutSize = renderImpl.TextLayoutSize;
            textDrawSize = renderImpl.TextDrawSize;
        }

        public IReadOnlyList<LyricDrawingTextGlyphRun> GlyphRuns => !disposedValue ? glyphRuns : throw new ObjectDisposedException(nameof(LyricDrawingTextGlyphRunGroup));

        public string PrimaryFontFamily { get; }

        public Size TextLayoutSize => !disposedValue ? textLayoutSize : throw new ObjectDisposedException(nameof(LyricDrawingTextGlyphRunGroup));

        public Size TextDrawSize => !disposedValue ? textDrawSize : throw new ObjectDisposedException(nameof(LyricDrawingTextGlyphRunGroup));

        public FontWeight FontWeight => !disposedValue ? fontWeight : throw new ObjectDisposedException(nameof(LyricDrawingTextGlyphRunGroup));

        public FontStyle FontStyle => !disposedValue ? fontStyle : throw new ObjectDisposedException(nameof(LyricDrawingTextGlyphRunGroup));

        public static LyricDrawingTextGlyphRunGroup Create(ICanvasResourceCreator resourceCreator, string textString, IReadOnlyList<CanvasFontFamily> fontFamilies, FontWeight fontWeight, FontStyle fontStyle)
        {
            return new LyricDrawingTextGlyphRunGroup(resourceCreator, textString, fontFamilies, fontWeight, fontStyle);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: 释放托管状态(托管对象)
                }

                var glyphRuns = this.glyphRuns;
                this.glyphRuns = null!;

                if (glyphRuns.Count > 0)
                {
                    var hash = new HashSet<CanvasFontFace>();

                    foreach (var item in glyphRuns)
                    {
                        if (item.FontFace != null)
                        {
                            hash.Add(item.FontFace);
                        }
                    }

                    foreach (var item in hash)
                    {
                        item.Dispose();
                    }
                }


                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
        }

        private class LyricTextLineRendererImpl : ICanvasTextRenderer
        {
            private CanvasTextLayout? textLayout;
            private List<LyricDrawingTextGlyphRun> glyphRuns;

            public LyricTextLineRendererImpl(ICanvasResourceCreator resourceCreator, string textString, IReadOnlyList<CanvasFontFamily> fontFamilies, FontWeight fontWeight, FontStyle fontStyle)
            {
                using (var textFormat = new CanvasTextFormat()
                {
                    FontFamily = null,
                    HorizontalAlignment = CanvasHorizontalAlignment.Left,
                    VerticalAlignment = CanvasVerticalAlignment.Top,
                    OpticalAlignment = CanvasOpticalAlignment.Default,
                    Direction = CanvasTextDirection.LeftToRightThenTopToBottom,
                    FontSize = 10,
                    FontStretch = FontStretch.Normal,
                    FontStyle = fontStyle,
                    FontWeight = fontWeight,
                    Options = CanvasDrawTextOptions.EnableColorFont,
                    WordWrapping = CanvasWordWrapping.NoWrap,
                })
                {
                    CanvasTextFormatHelper.SetFallbackFontFamilies(
                        textFormat,
                        fontFamilies,
                        CultureInfoUtils.DefaultUICulture.Name,
                        uri => new CanvasFontSet(uri));

                    PrimaryFontFamily = fontFamilies.First(c => c.IsMainFont).FontFamilyName;

                    using (var textLayout = new CanvasTextLayout(resourceCreator, textString, textFormat, 0, 0))
                    {
                        this.textLayout = textLayout;
                        glyphRuns = new List<LyricDrawingTextGlyphRun>();

                        var layoutBounds = textLayout.LayoutBounds;
                        var drawBounds = textLayout.DrawBounds;

                        TextLayoutSize = new Size(layoutBounds.Width, layoutBounds.Height);
                        TextDrawSize = new Size(Math.Max(0, drawBounds.Width), Math.Max(0, drawBounds.Height));

                        textLayout.DrawToTextRenderer(this, 0, 0);

                        this.textLayout = null;
                    }
                }
            }

            public IReadOnlyList<LyricDrawingTextGlyphRun> GlyphRuns => glyphRuns;

            public Size TextLayoutSize { get; private set; }

            public Size TextDrawSize { get; private set; }

            public string PrimaryFontFamily { get; private set; }

            public void DrawGlyphRun(Vector2 point, CanvasFontFace fontFace, float fontSize, CanvasGlyph[] glyphs, bool isSideways, uint bidiLevel, object brush, CanvasTextMeasuringMode measuringMode, string localeName, string textString, int[] clusterMapIndices, uint characterIndex, CanvasGlyphOrientation glyphOrientation)
            {
                if (glyphs == null || glyphs.Length == 0 || clusterMapIndices == null || clusterMapIndices.Length == 0) return;

                var glyphIndex = clusterMapIndices[0];
                var charIndex = 0;
                var x = point.X;
                var y = point.Y;

                var isEmoji = fontFace.FamilyNames.Values.Contains("Segoe UI Emoji");

                for (int i = 0; i < clusterMapIndices.Length; i++)
                {
                    var glyphCount = (i < clusterMapIndices.Length - 1) ? clusterMapIndices[i + 1] - glyphIndex : glyphs.Length - glyphIndex;

                    if (glyphCount > 0)
                    {
                        var curGlyphs = glyphs.Skip(glyphIndex).Take(glyphCount).ToArray();
                        var advance = curGlyphs.Sum(c => c.Advance);

                        textLayout!.GetCaretPosition((int)(charIndex + characterIndex), false, out var region);
                        
                        var glyphRun = new LyricDrawingTextGlyphRun()
                        {
                            Point = new Vector2(x, y),
                            FontFace = fontFace,
                            FontSize = fontSize,
                            Glyphs = curGlyphs,
                            IsSideways = isSideways,
                            BidiLevel = bidiLevel,
                            LocaleName = localeName,
                            TextString = textString.Substring(charIndex, i - charIndex + 1),
                            CharacterIndex = (uint)charIndex,
                            GlyphOrientation = glyphOrientation,
                            IsEmoji = isEmoji
                        };
                        glyphRuns.Add(glyphRun);

                        if (i < clusterMapIndices.Length - 1)
                        {
                            glyphIndex = clusterMapIndices[i + 1];
                            charIndex = i + 1;
                        }

                        if (isSideways
                            || glyphOrientation == CanvasGlyphOrientation.Clockwise90Degrees
                            || glyphOrientation == CanvasGlyphOrientation.Clockwise270Degrees)
                        {
                            if (bidiLevel % 2 == 0)
                            {
                                y += advance;
                            }
                            else
                            {
                                y -= advance;
                            }
                        }
                        else
                        {
                            if (bidiLevel % 2 == 0)
                            {
                                x += advance;
                            }
                            else
                            {
                                x -= advance;
                            }
                        }
                    }
                }
            }

            public void DrawStrikethrough(Vector2 point, float strikethroughWidth, float strikethroughThickness, float strikethroughOffset, CanvasTextDirection textDirection, object brush, CanvasTextMeasuringMode textMeasuringMode, string localeName, CanvasGlyphOrientation glyphOrientation)
            {
            }

            public void DrawUnderline(Vector2 point, float underlineWidth, float underlineThickness, float underlineOffset, float runHeight, CanvasTextDirection textDirection, object brush, CanvasTextMeasuringMode textMeasuringMode, string localeName, CanvasGlyphOrientation glyphOrientation)
            {
            }

            public void DrawInlineObject(Vector2 point, ICanvasTextInlineObject inlineObject, bool isSideways, bool isRightToLeft, object brush, CanvasGlyphOrientation glyphOrientation)
            {
            }

            public float Dpi => 96;

            public bool PixelSnappingDisabled => true;

            public Matrix3x2 Transform => Matrix3x2.Identity;
        }
    }
}
