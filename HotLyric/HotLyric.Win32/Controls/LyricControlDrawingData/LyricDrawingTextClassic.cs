using HotLyric.Win32.Utils;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Effects;
using Microsoft.Graphics.Canvas.Geometry;
using System.Collections.Generic;
using System.Numerics;
using Windows.Foundation;
using Windows.UI;

namespace HotLyric.Win32.Controls.LyricControlDrawingData
{
    internal class LyricDrawingTextClassic : LyricDrawingText
    {
        private readonly ICanvasResourceCreator resourceCreator;
        private readonly LyricDrawingTextColors colors;
        private readonly double scale;
        private readonly LyricControlProgressAnimationMode progressAnimationMode;
        private List<CanvasCachedGeometry> geometries;
        private Rect bounds;

        public LyricDrawingTextClassic(
            ICanvasResourceCreator resourceCreator,
            IReadOnlyList<LyricDrawingTextGlyphRun> lyricTextGlyphRuns,
            float strokeWidth,
            LyricDrawingTextColors colors,
            double scale,
            LyricDrawingLineTextSizeType sizeType,
            LyricControlProgressAnimationMode progressAnimationMode)
        {
            this.resourceCreator = resourceCreator;
            StrokeWidth = strokeWidth;
            this.colors = colors;
            this.scale = scale;
            this.progressAnimationMode = progressAnimationMode;
            geometries = new List<CanvasCachedGeometry>();

            CanvasGeometry? geometry = null;

            foreach (var glyphRun in lyricTextGlyphRuns)
            {
                var geo = CanvasGeometry.CreateGlyphRun(
                    resourceCreator,
                    glyphRun.Point,
                    glyphRun.FontFace,
                    glyphRun.FontSize,
                    glyphRun.Glyphs,
                    glyphRun.IsSideways,
                    glyphRun.BidiLevel,
                    Microsoft.Graphics.Canvas.Text.CanvasTextMeasuringMode.Natural,
                    glyphRun.GlyphOrientation);

                if (geometry == null)
                {
                    geometry = geo;
                }
                else
                {
                    var old = geometry;
                    geometry = geometry.CombineWith(geo, Matrix3x2.Identity, CanvasGeometryCombine.Union);
                    old.Dispose();
                    geo.Dispose();
                }
            }


            if (geometry != null)
            {
                geometry = geometry.Transform(Matrix3x2.CreateScale((float)scale));
                bounds = geometry.ComputeStrokeBounds(strokeWidth);

                if (sizeType == LyricDrawingLineTextSizeType.DrawSize)
                {
                    using (var old = geometry)
                    using (var old2 = old.Transform(Matrix3x2.CreateTranslation(-(float)bounds.Left, -(float)bounds.Top)))
                    using (var rectGeo = CanvasGeometry.CreateRectangle(resourceCreator, new Rect(0, 0, bounds.Width, bounds.Height)))
                    {
                        geometry = old2.CombineWith(
                            rectGeo,
                            Matrix3x2.Identity,
                            CanvasGeometryCombine.Intersect);
                    }
                    bounds.X = 0;
                    bounds.Y = 0;
                }

                geometries.Add(CanvasCachedGeometry.CreateFill(geometry));

                if (strokeWidth > 0)
                {
                    geometries.Add(CanvasCachedGeometry.CreateStroke(geometry, strokeWidth, new CanvasStrokeStyle()
                    {
                        TransformBehavior = strokeWidth > 1 ? CanvasStrokeTransformBehavior.Normal : CanvasStrokeTransformBehavior.Hairline
                    }));
                }


                geometry.Dispose();
            }
        }

        public float StrokeWidth { get; }

        protected override void DrawCore(CanvasDrawingSession drawingSession, double progress, bool lowFrameRateMode)
        {
            var originalProgress = progress;

            if(progressAnimationMode == LyricControlProgressAnimationMode.Disabled)
            {
                progress = 0;
            }

            if (progress > 0.001)
            {
                using (var commandList = new CanvasCommandList(drawingSession))
                {
                    using (var ds = commandList.CreateDrawingSession())
                    {
                        DrawCore(ds, colors.FillColor2, colors.StrokeColor2, colors.GlowColor2, progress, lowFrameRateMode);
                    }

                    var left = bounds.Left - 100 * scale;
                    var top = bounds.Top - 100 * scale;
                    var width = bounds.Width * progress + 100 * scale;
                    var height = bounds.Height + 200 * scale;

                    if (width <= 0 || height <= 0) return;

                    var sourceRect = new Rect(bounds.Left - 100 * scale, bounds.Top - 100 * scale, bounds.Width * progress + 100 * scale, bounds.Height + 200 * scale);
                    if (progress > 0.999)
                    {
                        sourceRect.Width += 100 * scale;
                    }

                    using (var cropEffect = new CropEffect()
                    {
                        Source = commandList,
                        SourceRectangle = sourceRect
                    })
                    {
                        drawingSession.DrawImage(cropEffect);
                    }
                }
            }
            if (progress < 0.999)
            {
                using (var commandList = new CanvasCommandList(drawingSession))
                {
                    using (var ds = commandList.CreateDrawingSession())
                    {
                        DrawCore(ds, colors.FillColor1, colors.StrokeColor1, colors.GlowColor1, progress, lowFrameRateMode);
                    }

                    var left = bounds.Left + bounds.Width * progress;
                    var top = bounds.Top - 100 * scale;
                    var width = bounds.Width + 100 * scale;
                    var height = bounds.Height + 200 * scale;

                    if (width <= 0 || height <= 0) return;

                    var sourceRect = new Rect(left, top, width, height);
                    if (progress < 0.001)
                    {
                        sourceRect.X -= 100 * scale;
                        sourceRect.Width += 100 * scale;
                    }

                    using (var cropEffect = new CropEffect()
                    {
                        Source = commandList,
                        SourceRectangle = sourceRect
                    })
                    {
                        drawingSession.DrawImage(cropEffect);
                    }
                }
            }
        }

        private void DrawCore(CanvasDrawingSession drawingSession, Color fillColor, Color strokeColor, Color glowColor, double progress, bool lowFrameRateMode)
        {
            if (!lowFrameRateMode)
            {
                using (var effectSource = new CanvasCommandList(drawingSession))
                {
                    using (var effectSourceDs = effectSource.CreateDrawingSession())
                    {
                        effectSourceDs.DrawCachedGeometry(geometries[0], glowColor);
                    }
                    var bounds = effectSource.GetBounds(drawingSession);

                    using (var effect = new GaussianBlurEffect()
                    {
                        Source = new Transform2DEffect()
                        {
                            Source = effectSource,
                            TransformMatrix = Matrix3x2.CreateTranslation(-(float)(bounds.Width * progress), 0)
                                * Matrix3x2.CreateScale(1.008f, 1)
                                * Matrix3x2.CreateTranslation((float)(bounds.Width * progress), 0)
                        },
                        BlurAmount = (float)scale / 3 * 2,
                        Optimization = EffectOptimization.Speed,
                    })
                    {
                        drawingSession.DrawImage(effect);
                    }
                }
            }

            if (geometries.Count > 0)
            {
                drawingSession.DrawCachedGeometry(geometries[0], fillColor);
            }
            if (geometries.Count > 1)
            {
                drawingSession.DrawCachedGeometry(geometries[1], strokeColor);
            }

#if DEBUG
            //drawingSession.FillRectangle(bounds, Color.FromArgb(40, 255, 0, 0));
#endif
        }

        protected override void DisposeCore(bool disposing)
        {
            var list = geometries;
            geometries = null!;

            foreach (var item in list)
            {
                item?.Dispose();
            }
        }
    }
}
