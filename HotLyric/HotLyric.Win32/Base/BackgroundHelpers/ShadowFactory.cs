using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Geometry;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using System.Windows;
using Windows.Perception.Spatial.Surfaces;
using Windows.UI;
using Windows.UI.Composition;
using Color = Windows.UI.Color;

namespace HotScreen.App.BackgroundHelpers
{
    public class ShadowFactory : IDisposable
    {
        public const double MaxShadowBlurRadius = 72.0;
        private readonly Compositor compositor;
        private Size cornerRadius;
        private System.Windows.Size size;
        //private CompositionSurfaceFactory? surfaceFactory;
        private CompositionClip shadowClip;
        private SpriteVisual shadowVisual;
        private ShapeVisual maskVisual;
        private CompositionRoundedRectangleGeometry maskGeometry;
        private CompositionSpriteShape maskShape;
        private CompositionVisualSurface maskSurface;
        private DropShadow shadow;
        private bool disposedValue;
        private double scaleRatio = 1d;
        private float shadowBlurRadius = 20f;
        private Vector3 shadowOffset;

        public ShadowFactory(Compositor compositor, System.Windows.Size size) : this(compositor, size, default) { }

        public ShadowFactory(Compositor compositor, System.Windows.Size size, Size cornerRadius)
        {
            if (compositor is null)
            {
                throw new ArgumentNullException(nameof(compositor));
            }

            if (size.Width < 0 || size.Height < 0)
            {
                throw new ArgumentException(nameof(size));
            }

            this.compositor = compositor;
            this.size = size;
            this.cornerRadius = cornerRadius;

            var scaledCornerRadius = new Vector2((float)(cornerRadius.Height * scaleRatio), (float)(cornerRadius.Height * scaleRatio));

            maskGeometry = compositor.CreateRoundedRectangleGeometry();
            maskGeometry.Size = new Vector2((float)(size.Width * scaleRatio), (float)(size.Height * scaleRatio));
            maskGeometry.CornerRadius = scaledCornerRadius;

            maskShape = compositor.CreateSpriteShape(maskGeometry);
            maskShape.FillBrush = compositor.CreateColorBrush(Color.FromArgb(255, 255, 255, 255));

            maskVisual = compositor.CreateShapeVisual();
            maskVisual.Size = maskGeometry.Size;
            maskVisual.Shapes.Add(maskShape);

            maskSurface = compositor.CreateVisualSurface();
            maskSurface.SourceVisual = maskVisual;
            maskSurface.SourceSize = maskVisual.Size;

            shadowClip = CreateShadowClip(compositor, scaledCornerRadius, new Vector2((float)(size.Width * scaleRatio), (float)(size.Height * scaleRatio)), (float)(MaxShadowBlurRadius * scaleRatio));

            shadow = compositor.CreateDropShadow();
            shadow.BlurRadius = 64f;
            shadow.Mask = compositor.CreateSurfaceBrush(maskSurface);
            shadow.Opacity = 0.4f;
            shadow.Offset = new Vector3(0, 10, 0);

            shadowVisual = compositor.CreateSpriteVisual();
            shadowVisual.Size = new Vector2((float)size.Width, (float)size.Height);
            shadowVisual.Shadow = shadow;
            shadowVisual.Clip = shadowClip;
        }

        public System.Windows.Size Size
        {
            get => size;
            set
            {
                if (size != value)
                {
                    if (value.Width < 1 || value.Height < 1)
                    {
                        throw new ArgumentException(nameof(size));
                    }

                    size = value;

                    ResetProperties();
                }
            }
        }

        public Size CornerRadius
        {
            get => cornerRadius;
            set
            {
                if (cornerRadius != value)
                {
                    cornerRadius = value;

                    ResetProperties();
                }
            }
        }

        public float ShadowBlurRadius
        {
            get => shadowBlurRadius;
            set
            {
                if (shadowBlurRadius != value)
                {
                    shadowBlurRadius = value;
                    shadow!.BlurRadius = (float)(value * scaleRatio);
                }
            }
        }

        public Color ShadowColor
        {
            get => shadow!.Color;
            set => shadow!.Color = value;
        }

        public Vector3 ShadowOffset
        {
            get => shadowOffset;
            set
            {
                if (shadowOffset != value)
                {
                    shadowOffset = value;
                    shadow!.Offset = value * (float)ScaleRatio;
                }
            }
        }

        public double ShadowOpacity
        {
            get => shadow!.Opacity;
            set => shadow!.Opacity = (float)value;
        }

        public double ScaleRatio
        {
            get => scaleRatio;
            set
            {
                if (scaleRatio != value)
                {
                    scaleRatio = value;
                    ResetProperties();
                }
            }
        }

        public Visual ShadowVisual => shadowVisual;

        private void ResetProperties()
        {
            var scaledCornerRadius = new Vector2((float)(cornerRadius.Width * scaleRatio), (float)(cornerRadius.Height * scaleRatio));
            var vSize = new Vector2((float)(size.Width * scaleRatio), (float)(size.Height * scaleRatio));

            maskGeometry.Size = vSize;
            maskGeometry.CornerRadius = scaledCornerRadius;
            maskVisual.Size = vSize;
            maskSurface.SourceSize = vSize;

            shadowVisual.Size = new Vector2((float)(size.Width * scaleRatio), (float)(size.Height * scaleRatio));

            shadowClip = CreateShadowClip(compositor, scaledCornerRadius, new Vector2((float)(size.Width * scaleRatio), (float)(size.Height * scaleRatio)), (float)(MaxShadowBlurRadius * scaleRatio));
            var oldClip = shadowVisual.Clip;
            shadowVisual.Clip = shadowClip;
            oldClip?.Dispose();

            shadow.BlurRadius = (float)(ShadowBlurRadius * ScaleRatio);
            shadow!.Offset = shadowOffset * (float)ScaleRatio;

            PropertyChanged?.Invoke(this, EventArgs.Empty);
        }

        public event EventHandler? PropertyChanged;

        internal static CompositionClip CreateVisualClip(Compositor compositor, Vector2 cornerRadius, Vector2 size)
        {
            var geometry = CreateRoundedRectangle(cornerRadius, new Rect(0, 0, size.X, size.Y));

            var compositionPath = new CompositionPath(geometry);

            var compositionGeometry = compositor.CreatePathGeometry(compositionPath);

            var clip = compositor.CreateGeometricClip(compositionGeometry);

            return clip;
        }

        public static CompositionClip CreateShadowClip(Compositor compositor, Vector2 cornerRadius, Vector2 size, float blurRadius)
        {
            var geometry = CreateRoundedRectangle(cornerRadius, new Rect(0, 0, size.X, size.Y));

            var geo2 = CanvasGeometry.CreateRectangle(
                null,
                new Windows.Foundation.Rect(-blurRadius * 3, -blurRadius * 3, size.X + blurRadius * 6, size.Y + blurRadius * 6));

            var geo = geo2.CombineWith(geometry, Matrix3x2.Identity, CanvasGeometryCombine.Exclude);

            var compositionPath = new CompositionPath(geo);

            var compositionGeometry = compositor.CreatePathGeometry(compositionPath);

            var clip = compositor.CreateGeometricClip(compositionGeometry);

            return clip;
        }

        private static CanvasGeometry CreateRoundedRectangle(Vector2 cornerRadius, Rect rect) =>
            CreateRoundedRectangle(null, cornerRadius, rect);

        private static CanvasGeometry CreateRoundedRectangle(ICanvasResourceCreator? resourceCreator, Vector2 cornerRadius, Rect rect)
        {
            var normalizedCR = NormalizeCornerRadius(cornerRadius, rect);

            var pathBuilder = new CanvasPathBuilder(resourceCreator);

            pathBuilder.BeginFigure(new Vector2((float)(rect.Left + normalizedCR.X), 0));
            pathBuilder.AddLine(new Vector2((float)(rect.Right - normalizedCR.X), 0));
            pathBuilder.AddArc(
                new Vector2((float)(rect.Right - normalizedCR.X), (float)normalizedCR.Y),
                (float)normalizedCR.X,
                (float)normalizedCR.Y,
                -MathF.PI / 2,
                MathF.PI / 2);

            pathBuilder.AddLine(new Vector2((float)rect.Right, (float)(rect.Bottom - normalizedCR.Y)));
            pathBuilder.AddArc(
                new Vector2((float)rect.Right - (float)normalizedCR.X, (float)(rect.Bottom - normalizedCR.Y)),
                (float)normalizedCR.X,
                (float)normalizedCR.Y,
                0,
                MathF.PI / 2);

            pathBuilder.AddLine(new Vector2((float)(rect.Left + normalizedCR.X), (float)rect.Bottom));
            pathBuilder.AddArc(
                new Vector2((float)normalizedCR.X, (float)(rect.Bottom - normalizedCR.Y)),
                (float)normalizedCR.X,
                (float)normalizedCR.Y,
                MathF.PI / 2,
                MathF.PI / 2);

            pathBuilder.AddLine(new Vector2((float)rect.Left, (float)(rect.Top + normalizedCR.Y)));
            pathBuilder.AddArc(
                new Vector2((float)(rect.Left + normalizedCR.X), (float)(rect.Top + normalizedCR.Y)),
                (float)normalizedCR.X,
                (float)normalizedCR.Y,
                MathF.PI,
                MathF.PI / 2);

            pathBuilder.EndFigure(CanvasFigureLoop.Closed);

            var geometry = CanvasGeometry.CreatePath(pathBuilder);

            return geometry;
        }

        private static Vector2 NormalizeCornerRadius(Vector2 cornerRadius, Rect rect)
        {
            if (cornerRadius.X * 2 > rect.Width)
            {
                cornerRadius.X = (float)(rect.Width / 2);
            }


            if (cornerRadius.Y * 2 > rect.Height)
            {
                cornerRadius.Y = (float)(rect.Height / 2);
            }

            return cornerRadius;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: 释放托管状态(托管对象)

                    shadowVisual?.Dispose();
                    shadowVisual = null!;

                    shadow?.Dispose();
                    shadow = null!;

                    shadowClip?.Dispose();
                    shadowClip = null!;

                    maskSurface?.Dispose();
                    maskSurface = null!;

                    maskVisual?.Dispose();
                    maskVisual = null!;

                    maskShape?.Dispose();
                    maskShape = null!;

                    maskGeometry?.Dispose();
                    maskGeometry = null!;
                }

                // TODO: 释放未托管的资源(未托管的对象)并重写终结器
                // TODO: 将大型字段设置为 null
                disposedValue = true;
            }
        }

        // // TODO: 仅当“Dispose(bool disposing)”拥有用于释放未托管资源的代码时才替代终结器
        // ~ShadowFactory()
        // {
        //     // 不要更改此代码。请将清理代码放入“Dispose(bool disposing)”方法中
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            // 不要更改此代码。请将清理代码放入“Dispose(bool disposing)”方法中
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
