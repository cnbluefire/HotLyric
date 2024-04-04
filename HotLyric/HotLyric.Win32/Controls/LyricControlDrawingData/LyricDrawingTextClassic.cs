using BlueFire.Toolkit.WinUI3.Text;
using HotLyric.Win32.Controls.LyricControlDrawingData.DrawAnimations;
using HotLyric.Win32.Utils;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Brushes;
using Microsoft.Graphics.Canvas.Effects;
using Microsoft.Graphics.Canvas.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Windows.Foundation;
using Windows.UI;

namespace HotLyric.Win32.Controls.LyricControlDrawingData
{
    internal class LyricDrawingTextClassic : LyricDrawingText
    {
        private readonly ICanvasResourceCreator resourceCreator;
        private readonly double scale;
        private List<CanvasCachedGeometry> geometries;
        private Rect bounds;

        public LyricDrawingTextClassic(
            ICanvasResourceCreator resourceCreator,
            FormattedText formattedText,
            float strokeWidth,
            double scale,
            LyricDrawingLineTextSizeType sizeType)
        {
            this.resourceCreator = resourceCreator;
            StrokeWidth = strokeWidth;
            this.scale = scale;
            geometries = new List<CanvasCachedGeometry>();

            CanvasGeometry? geometry = null;

            using var textLayout = formattedText.CreateCanvasTextLayout(resourceCreator);

            geometry = CanvasGeometry.CreateText(textLayout);

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

        protected override void DrawCore(CanvasDrawingSession drawingSession, in LyricDrawingParameters parameters)
        {
            var progress = parameters.PlayProgress;
            var colors = parameters.Colors;

            var fillGeometry = geometries[0];
            var strokeGeometry = geometries.Count > 1 ? geometries[1] : null;

            if (parameters.ProgressAnimationMode == LyricControlProgressAnimationMode.Disabled)
            {
                DrawGlow(drawingSession, fillGeometry, 0.5, colors.GlowColor1, scale, parameters.LowFrameRateMode);
                DrawText(drawingSession, fillGeometry, strokeGeometry, colors.FillColor1, colors.StrokeColor1);
            }
            else
            {
                if (progress > 0.001)
                {
                    var left = bounds.Left - 100 * scale;
                    var top = bounds.Top - 100 * scale;
                    var width = bounds.Width * progress + 100 * scale;
                    var height = bounds.Height + 200 * scale;

                    if (width > 0 && height > 0)
                    {
                        var sourceRect = new Rect(left, top, width, height);
                        if (progress > 0.999)
                        {
                            sourceRect.Width += 100 * scale;
                        }

                        using (var layer = drawingSession.CreateLayer(1, sourceRect))
                        {
                            DrawGlow(drawingSession, fillGeometry, progress, colors.GlowColor2, scale, parameters.LowFrameRateMode);

                            if (progress > 0.999 || parameters.LowFrameRateMode)
                            {
                                DrawText(drawingSession, fillGeometry, strokeGeometry, colors.FillColor2, colors.StrokeColor2);
                            }
                        }
                    }
                }
                if (progress < 0.999)
                {
                    var left = bounds.Left + bounds.Width * progress;
                    var top = bounds.Top - 100 * scale;
                    var width = bounds.Width + 100 * scale;
                    var height = bounds.Height + 200 * scale;

                    if (width > 0 && height > 0)
                    {
                        var sourceRect = new Rect(left, top, width, height);
                        if (progress < 0.001)
                        {
                            sourceRect.X -= 100 * scale;
                            sourceRect.Width += 100 * scale;
                        }

                        using (var layer = drawingSession.CreateLayer(1, sourceRect))
                        {
                            DrawGlow(drawingSession, fillGeometry, progress, colors.GlowColor1, scale, parameters.LowFrameRateMode);

                            if (progress < 0.001 || parameters.LowFrameRateMode)
                            {
                                DrawText(drawingSession, fillGeometry, strokeGeometry, colors.FillColor1, colors.StrokeColor1);
                            }
                        }
                    }
                }

                if (!parameters.LowFrameRateMode && progress >= 0.001 && progress <= 0.999)
                {
                    const double GradientWidth = 3d;

                    using (var holder = LyricDrawingKaraokeGradientStopsPool.Rent())
                    {
                        var stops = holder.Stops;

                        stops[0] = new CanvasGradientStop(0, colors.FillColor2);
                        stops[1] = new CanvasGradientStop((float)progress, colors.FillColor2);
                        stops[2] = new CanvasGradientStop((float)Math.Min(progress + GradientWidth * scale / bounds.Width, 1), colors.FillColor1);
                        stops[3] = new CanvasGradientStop(1, colors.FillColor1);

                        using (var brush = new CanvasLinearGradientBrush(drawingSession, stops))
                        {
                            brush.StartPoint = new Vector2((float)bounds.Left, (float)bounds.Top);
                            brush.EndPoint = new Vector2((float)bounds.Right, (float)bounds.Top);

                            drawingSession.DrawCachedGeometry(fillGeometry, brush);
                        }

                        if (strokeGeometry != null)
                        {
                            stops[0].Color = colors.StrokeColor2;
                            stops[1].Color = colors.StrokeColor2;
                            stops[2].Color = colors.StrokeColor1;
                            stops[3].Color = colors.StrokeColor1;

                            using (var brush = new CanvasLinearGradientBrush(drawingSession, stops))
                            {
                                brush.StartPoint = new Vector2((float)bounds.Left, (float)bounds.Top);
                                brush.EndPoint = new Vector2((float)bounds.Right, (float)bounds.Top);

                                drawingSession.DrawCachedGeometry(strokeGeometry, brush);
                            }
                        }
                    }
                }
            }
        }

        private static void DrawGlow(CanvasDrawingSession drawingSession, CanvasCachedGeometry fillGeometry, double progress, Color glowColor, double scale, bool lowFrameRateMode)
        {
            if (!lowFrameRateMode)
            {
                using (var effectSource = new CanvasCommandList(drawingSession))
                {
                    using (var effectSourceDs = effectSource.CreateDrawingSession())
                    {
                        effectSourceDs.DrawCachedGeometry(fillGeometry, glowColor);
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
        }

        private static void DrawText(CanvasDrawingSession drawingSession, CanvasCachedGeometry fillGeometry, CanvasCachedGeometry? strokeGeometry, Color fillColor, Color strokeColor)
        {
            drawingSession.DrawCachedGeometry(fillGeometry, fillColor);

            if (strokeGeometry != null)
            {
                drawingSession.DrawCachedGeometry(strokeGeometry, strokeColor);
            }
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
