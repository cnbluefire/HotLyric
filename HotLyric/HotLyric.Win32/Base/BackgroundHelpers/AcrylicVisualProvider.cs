using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Windows.UI;
using Windows.UI.Composition;
using Color = Windows.UI.Color;

namespace HotScreen.App.BackgroundHelpers
{
    public class AcrylicVisualProvider : IDisposable
    {
        private bool disposedValue;
        private Compositor compositor;
        private System.Windows.Size size;
        private Size cornerRadius;
        private System.Windows.Point offset;
        private ShadowFactory? shadowFactory;
        private ContainerVisual containerVisual;
        private SpriteVisual acrylicVisual;
        private AcrylicHelper? acrylicHelper;
        private double scaleRatio = 1d;

        private AcrylicVisualProvider(Compositor compositor, System.Windows.Size size, Size cornerRadius)
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

            if (Extensions.IsShadowSupported)
            {
                shadowFactory = new ShadowFactory(compositor, size, cornerRadius);
            }

            var vSize = new Vector2((float)(size.Width * scaleRatio), (float)(size.Height * scaleRatio));

            acrylicVisual = compositor.CreateSpriteVisual();
            acrylicVisual.Size = vSize;
            acrylicVisual.Clip = CreateVisualClip();


            containerVisual = compositor.CreateContainerVisual();
            containerVisual.Size = vSize;

            containerVisual.Children.InsertAtTop(acrylicVisual);

            if (shadowFactory != null)
            {
                var weakAcrylicVisual = new WeakReference<SpriteVisual>(acrylicVisual);
                shadowFactory.PropertyChanged += (s, a) =>
                {
                    if (weakAcrylicVisual.TryGetTarget(out var _v))
                    {
                        _v.Clip = CreateVisualClip();
                    }
                };

                containerVisual.Children.InsertAtTop(shadowFactory.ShadowVisual);
            }
        }

        private async Task InitAcrylicBrushAsync()
        {
            acrylicHelper = await AcrylicHelper.CreateAsync();

            acrylicHelper!.TintColor = AcrylicHelper.DefaultTintColor;
            acrylicHelper!.FallbackColor = AcrylicHelper.DefaultFallbackColor;

            acrylicHelper!.TintOpacity = AcrylicHelper.DefaultTintOpacity;
            acrylicHelper!.TintLuminosityOpacity = AcrylicHelper.DefaultTintLuminosityOpacity;

            acrylicVisual.Brush = acrylicHelper!.Brush;
        }

        public Visual Visual => containerVisual;

        public double Opacity
        {
            get => containerVisual.Opacity;
            set => containerVisual.Opacity = (float)value;
        }

        public System.Windows.Size CornerRadius
        {
            get => cornerRadius;
            set
            {
                if (cornerRadius != value)
                {
                    cornerRadius = value;
                }

                acrylicVisual.Clip = CreateVisualClip();
                if (shadowFactory != null)
                {
                    shadowFactory.CornerRadius = value;
                }
            }
        }

        public System.Windows.Size Size
        {
            get => size;
            set
            {
                if (size != value)
                {
                    if (value.Width < 0 || value.Height < 0)
                    {
                        throw new ArgumentException(nameof(size));
                    }

                    size = value;

                    UpdateSize();
                }
            }
        }

        public System.Windows.Point Offset
        {
            get => offset;
            set
            {
                if (offset != value)
                {
                    offset = value;

                    UpdateOffset();
                }
            }
        }

        public Color TintColor
        {
            get => acrylicHelper!.TintColor;
            set => acrylicHelper!.TintColor = value;
        }

        public double TintOpacity
        {
            get => acrylicHelper!.TintOpacity;
            set => acrylicHelper!.TintOpacity = value;
        }

        public double? TintLuminosityOpacity
        {
            get => acrylicHelper!.TintLuminosityOpacity;
            set => acrylicHelper!.TintLuminosityOpacity = value;
        }

        public Color FallbackColor
        {
            get => acrylicHelper!.FallbackColor;
            set => acrylicHelper!.FallbackColor = value;
        }

        public bool UseFallback
        {
            get => acrylicHelper!.UseFallback;
            set => acrylicHelper!.UseFallback = value;
        }

        public double BlurAmount
        {
            get => acrylicHelper!.BlurAmount;
            set => acrylicHelper!.BlurAmount = value;
        }

        public double ScaleRatio
        {
            get => scaleRatio;
            set
            {
                if (scaleRatio != value)
                {
                    scaleRatio = value;

                    acrylicHelper!.NoiseScaleRatio = value;
                    shadowFactory!.ScaleRatio = value;

                    UpdateOffset();
                    UpdateSize();
                }
            }
        }






        public float ShadowBlurRadius
        {
            get => shadowFactory?.ShadowBlurRadius ?? 0;
            set { if (shadowFactory != null) shadowFactory.ShadowBlurRadius = value; }
        }

        public Color ShadowColor
        {
            get => shadowFactory?.ShadowColor ?? Color.FromArgb(255, 0, 0, 0);
            set { if (shadowFactory != null) shadowFactory.ShadowColor = value; }
        }

        public Vector3 ShadowOffset
        {
            get => shadowFactory?.ShadowOffset ?? Vector3.Zero;
            set { if (shadowFactory != null) shadowFactory.ShadowOffset = value; }
        }

        public double ShadowOpacity
        {
            get => shadowFactory?.ShadowOpacity ?? 0;
            set { if (shadowFactory != null) shadowFactory.ShadowOpacity = value; }
        }

        public bool IsShadowVisible
        {
            get => shadowFactory?.ShadowVisual?.IsVisible ?? false;
            set { if (shadowFactory?.ShadowVisual != null) shadowFactory.ShadowVisual.IsVisible = value; }
        }

        private void UpdateSize()
        {
            var vSize = new Vector2((float)(size.Width * scaleRatio), (float)(size.Height * scaleRatio));

            if (shadowFactory != null)
            {
                shadowFactory.Size = size;
            }
            containerVisual.Size = vSize;
            acrylicVisual.Size = vSize;
        }

        private void UpdateOffset()
        {
            var vOffset = new Vector3((float)(offset.X * scaleRatio), (float)(offset.Y * scaleRatio), 0);
            containerVisual.Offset = vOffset;
        }
        private CompositionClip CreateVisualClip()
        {
            var scaledCornerRadius = new Vector2((float)(cornerRadius.Width * scaleRatio), (float)(cornerRadius.Height * scaleRatio));
            return ShadowFactory.CreateVisualClip(compositor, scaledCornerRadius, new Vector2((float)(size.Width * scaleRatio), (float)(size.Height * scaleRatio)));

        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: 释放托管状态(托管对象)

                    containerVisual.Parent.Children.Remove(containerVisual);
                    containerVisual.Children.RemoveAll();
                    containerVisual.Dispose();
                    containerVisual = null!;

                    acrylicVisual.Dispose();
                    acrylicVisual = null!;

                    shadowFactory?.Dispose();
                    shadowFactory = null;

                    acrylicHelper?.Dispose();
                    acrylicHelper = null;
                }

                // TODO: 释放未托管的资源(未托管的对象)并重写终结器
                // TODO: 将大型字段设置为 null
                disposedValue = true;
            }
        }

        // // TODO: 仅当“Dispose(bool disposing)”拥有用于释放未托管资源的代码时才替代终结器
        // ~AcrylicFactory()
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

        public static async Task<AcrylicVisualProvider> CreateAsync(Compositor compositor, System.Windows.Size size, Size cornerRadius)
        {
            var provider = new AcrylicVisualProvider(compositor, size, cornerRadius);
            await provider.InitAcrylicBrushAsync();

            return provider;
        }
    }
}
