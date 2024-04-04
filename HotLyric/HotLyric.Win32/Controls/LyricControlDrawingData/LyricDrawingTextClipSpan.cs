using BlueFire.Toolkit.WinUI3.Text;
using HotLyric.Win32.Utils;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Effects;
using Microsoft.Graphics.Canvas.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using Windows.Foundation;
using Windows.UI;

namespace HotLyric.Win32.Controls.LyricControlDrawingData
{
    internal class LyricDrawingTextClipSpan : LyricDrawingText
    {
        private readonly ICanvasResourceCreator resourceCreator;
        private readonly double scale;
        private readonly LyricDrawingLineTextSizeType sizeType;
        private List<(CanvasCachedGeometry fill, CanvasCachedGeometry? stroke, double startProgress, double endProgrsss)> geometries;
        private Rect bounds;

        public LyricDrawingTextClipSpan(
            ICanvasResourceCreator resourceCreator,
            FormattedText formattedText,
            float strokeWidth,
            double scale,
            LyricDrawingLineTextSizeType sizeType)
        {
            this.resourceCreator = resourceCreator;
            StrokeWidth = strokeWidth;
            this.scale = scale;
            this.sizeType = sizeType;

            geometries = new List<(CanvasCachedGeometry fill, CanvasCachedGeometry? stroke, double startProgress, double endProgrsss)>();

            var lyricTextGlyphRuns = formattedText.LineGlyphRuns
                .SelectMany(c => c.GlyphRuns)
                .ToList();

            var unit = lyricTextGlyphRuns.Count > 0 ? 1d / lyricTextGlyphRuns.Count : 1;

            Rect? strokeBounds = null;

            for (int i = 0; i < lyricTextGlyphRuns.Count; i++)
            {
                var glyphRun = lyricTextGlyphRuns[i];
                using (var geometry = CanvasGeometry.CreateGlyphRun(
                    resourceCreator,
                    glyphRun.Point,
                    glyphRun.FontFace,
                    glyphRun.FontSize,
                    glyphRun.Glyphs,
                    glyphRun.IsSideways,
                    glyphRun.BidiLevel,
                    Microsoft.Graphics.Canvas.Text.CanvasTextMeasuringMode.Natural,
                    glyphRun.GlyphOrientation))
                {
                    var tmpStrokeBounds = geometry.ComputeStrokeBounds(strokeWidth);

                    if (strokeBounds.HasValue)
                    {
                        tmpStrokeBounds.Union(strokeBounds.Value);
                    }
                    strokeBounds = tmpStrokeBounds;
                }
            }

            bounds = strokeBounds ?? new Rect();

            var offsetX = 0f;
            var offsetY = 0f;

            if (sizeType == LyricDrawingLineTextSizeType.DrawSize)
            {
                offsetX = -(float)bounds.Left;
                offsetY = -(float)bounds.Top;

                bounds.X = 0;
                bounds.Y = 0;
            }

            for (var i = 0; i < lyricTextGlyphRuns.Count; i++)
            {
                var glyphRun = lyricTextGlyphRuns[i];

                var geometry = CanvasGeometry.CreateGlyphRun(
                    resourceCreator,
                    glyphRun.Point,
                    glyphRun.FontFace,
                    glyphRun.FontSize,
                    glyphRun.Glyphs,
                    glyphRun.IsSideways,
                    glyphRun.BidiLevel,
                    Microsoft.Graphics.Canvas.Text.CanvasTextMeasuringMode.Natural,
                    glyphRun.GlyphOrientation);

                var old = geometry;
                geometry = geometry.Transform(
                    Matrix3x2.CreateTranslation(offsetX, offsetY)
                    * Matrix3x2.CreateScale((float)scale));

                CanvasCachedGeometry fill = CanvasCachedGeometry.CreateFill(geometry);
                CanvasCachedGeometry? stroke = null;

                if (strokeWidth > 0)
                {
                    stroke = CanvasCachedGeometry.CreateStroke(geometry, strokeWidth);
                }

                geometries.Add((fill, stroke, unit * i, i == lyricTextGlyphRuns.Count ? 1 : unit * (i + 1)));

                old.Dispose();
                geometry.Dispose();
            }
        }

        public float StrokeWidth { get; }

        protected override void DrawCore(CanvasDrawingSession drawingSession, in LyricDrawingParameters parameters)
        {
            foreach (var (fill, stroke, start, end) in geometries)
            {
                if (parameters.PlayProgress > start)
                {
                    DrawCore2(drawingSession, fill, stroke, MapProgress(parameters.PlayProgress, start, end), parameters.Colors, parameters.LowFrameRateMode);
                }
                else
                {
                    DrawCore1(drawingSession, fill, stroke, parameters.Colors, parameters.LowFrameRateMode);
                }
            }
        }

        protected void DrawCore1(CanvasDrawingSession drawingSession, CanvasCachedGeometry fillGeometry, CanvasCachedGeometry? strokeGeometry, LyricDrawingTextColors colors, bool lowFrameRateMode)
        {
            if (!lowFrameRateMode)
            {
                using (var commandList = new CanvasCommandList(drawingSession))
                {
                    using (var ds = commandList.CreateDrawingSession())
                    {
                        var glowColor = colors.GlowColor1;
                        ds.DrawCachedGeometry(fillGeometry, glowColor);
                    }
                    var effect = new GaussianBlurEffect()
                    {
                        Source = commandList,
                        BlurAmount = (float)(scale / 3 * 2 * 0.6),
                        Optimization = EffectOptimization.Speed,
                    };

                    drawingSession.DrawImage(effect);
                }
            }

            drawingSession.DrawCachedGeometry(fillGeometry, colors.FillColor1);

            if (strokeGeometry != null)
            {
                drawingSession.DrawCachedGeometry(strokeGeometry, colors.StrokeColor1);
            }
        }

        protected void DrawCore2(CanvasDrawingSession drawingSession, CanvasCachedGeometry fillGeometry, CanvasCachedGeometry? strokeGeometry, double progress, LyricDrawingTextColors colors, bool lowFrameRateMode)
        {
            var offsetY = (float)(-0.6 * progress * scale);

            if (!lowFrameRateMode)
            {
                using (var commandList = new CanvasCommandList(drawingSession))
                {
                    using (var ds = commandList.CreateDrawingSession())
                    {
                        var glowColor = ColorExtensions.CompositeColor(colors.GlowColor1, colors.GlowColor2, progress);
                        ds.DrawCachedGeometry(fillGeometry, glowColor);
                    }
                    var effect = new GaussianBlurEffect()
                    {
                        Source = commandList,
                        BlurAmount = (float)(scale / 3 * 2 * (progress * 0.4 + 0.6)),
                        Optimization = EffectOptimization.Speed,
                    };

                    drawingSession.DrawImage(effect, new Vector2(0, offsetY));
                }
            }

            var fillColor = ColorExtensions.CompositeColor(colors.FillColor1, colors.FillColor2, progress);
            drawingSession.DrawCachedGeometry(fillGeometry, new Vector2(0, offsetY), fillColor);

            if (strokeGeometry != null)
            {
                var strokeColor = ColorExtensions.CompositeColor(colors.StrokeColor1, colors.StrokeColor2, progress);
                drawingSession.DrawCachedGeometry(strokeGeometry, new Vector2(0, offsetY), strokeColor);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private double MapProgress(double progress, double rangeStart, double rangeEnd)
        {
            return Math.Max(0, Math.Min(1, (progress - rangeStart) / (rangeEnd - rangeStart)));
        }
    }
}
