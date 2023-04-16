using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vanara.PInvoke;
using Windows.UI.Composition.Desktop;
using WinUIEx;
using WinRT;
using System.Reflection.Metadata;
using Windows.UI.Composition;
using System.Numerics;
using Microsoft.Graphics.Canvas.Geometry;
using HotLyric.Win32.Utils;
using HotLyric.Win32.Base.BackgroundHelpers;
using Microsoft.UI.Xaml;

namespace HotLyric.Win32.Base
{
    public class WindowAcrylicContext : IDisposable
    {
        private const float MaxBlurRadius = 72;
        private readonly WindowAcrylicController windowAcrylicController;

        private bool disposeValue;

        private Compositor compositor;

        private Window? window;
        private WindowManager? windowManager;
        private DesktopWindowTarget? desktopWindowTarget;

        private ContainerVisual rootVisual;

        private SpriteVisual? backgroundShadowHostVisual;
        private DropShadow? backgroundShadow;

        private CompositionGeometricClip? backgroundShadowClip;
        private CompositionPathGeometry? backgroundShadowClipGeometry;

        private CompositionPathGeometry? backgroundShadowHostGeometry;
        private CompositionSpriteShape? backgroundShadowHostSurfaceShape;
        private ShapeVisual? backgroundShadowHostSurfaceVisual;
        private CompositionVisualSurface? backgroundShadowHostSurface;
        private CompositionSurfaceBrush? backgroundShadowHostSurfaceBrush;

        private CompositionVisualSurface backdropSurface;
        private ContainerVisual blurAndShadowContainer;

        private CompositionPathGeometry? hostBackdropGeometry;
        private SpriteVisual? hostBackdropVisual;

        private Thickness padding = new Thickness(10);
        private double cornerRadius = 8;

        private AcrylicHelper? acrylicHelper;

        public WindowAcrylicContext(WindowAcrylicController windowAcrylicController)
        {
            this.windowAcrylicController = windowAcrylicController;
            compositor = WindowsCompositionHelper.Compositor;

            rootVisual = compositor.CreateContainerVisual();
            rootVisual.RelativeSizeAdjustment = Vector2.One;

            backdropSurface = WindowsCompositionHelper.Compositor.CreateVisualSurface();
            blurAndShadowContainer = WindowsCompositionHelper.Compositor.CreateContainerVisual();

            InitializeLayoutRootShadow();
            InitializeAcrylic();

            blurAndShadowContainer.Children.InsertAtTop(backgroundShadowHostVisual);
            blurAndShadowContainer.Children.InsertAtTop(hostBackdropVisual);

            rootVisual.Children.InsertAtTop(blurAndShadowContainer);

            var imp = compositor.CreateImplicitAnimationCollection();
            var opacityAn = compositor.CreateScalarKeyFrameAnimation();
            opacityAn.InsertExpressionKeyFrame(1, "this.FinalValue");
            opacityAn.Duration = TimeSpan.FromSeconds(0.2);
            opacityAn.Target = "Opacity";
            imp[opacityAn.Target] = opacityAn;
            blurAndShadowContainer.ImplicitAnimations = imp;
        }

        public Window? Window
        {
            get => window;
            set
            {
                if (window != value)
                {
                    RemoveWindow();
                    window = value;
                    InitWindow();
                }
            }
        }

        public Thickness Padding
        {
            get => padding;
            set
            {
                if (padding != value)
                {
                    padding = value;
                    UpdateVisualSize();
                }
            }
        }

        public double CornerRadius
        {
            get => cornerRadius;
            set
            {
                if (cornerRadius != value)
                {
                    cornerRadius = value;
                    UpdateVisualSize();
                }
            }
        }

        public double Opacity
        {
            get => blurAndShadowContainer.Opacity;
            set => blurAndShadowContainer.Opacity = (float)value;
        }

        public void UpdateShadowProperties()
        {
            if (backgroundShadow != null)
            {
                backgroundShadow.Offset = new Vector3(
                    (float)windowAcrylicController.ShadowOffsetX,
                    (float)windowAcrylicController.ShadowOffsetY,
                    0);

                backgroundShadow.Color = windowAcrylicController.ShadowColor;
                backgroundShadow.Opacity = (float)windowAcrylicController.ShadowOpacity;
            }

        }

        public void UpdateAcrylicProperties()
        {
            var brush = windowAcrylicController.AcrylicBrush;

            if (brush == null)
            {
                if (hostBackdropVisual != null)
                {
                    hostBackdropVisual.IsVisible = false;
                }
            }
            else
            {
                if (acrylicHelper != null)
                {
                    acrylicHelper.TintColor = brush.TintColor;
                    acrylicHelper.FallbackColor = brush.FallbackColor;
                    acrylicHelper.UseFallback = brush.AlwaysUseFallback;
                    acrylicHelper.TintOpacity = brush.TintOpacity;
                    acrylicHelper.TintLuminosityOpacity = brush.TintLuminosityOpacity;
                }

                if (hostBackdropVisual != null)
                {
                    hostBackdropVisual.Opacity = (float)brush.Opacity;
                    hostBackdropVisual.IsVisible = true;
                }
            }
        }

        private void InitWindow()
        {
            if (window != null)
            {
                var handle = (nint)window.GetAppWindow().Id.Value;
                desktopWindowTarget = CreateDesktopWindowTarget(handle);
                windowManager = WindowManager.Get(window);
                windowManager.WindowMessageReceived += WindowManager_WindowMessageReceived;

                desktopWindowTarget.Root = rootVisual;

                UpdateRootVisualScale();
                UpdateRootVisualSize();

                var brushHolder = window.As<Microsoft.UI.Composition.ICompositionSupportsSystemBackdrop>();
                brushHolder.SystemBackdrop = compositor.CreateColorBrush(Windows.UI.Color.FromArgb(0, 255, 255, 255));
            }
        }

        private void RemoveWindow()
        {
            if (window != null)
            {
                var brushHolder = window.As<Microsoft.UI.Composition.ICompositionSupportsSystemBackdrop>();
                brushHolder.SystemBackdrop = null;


                if (windowManager != null)
                {
                    windowManager.WindowMessageReceived -= WindowManager_WindowMessageReceived;
                    windowManager = null;
                }

                if (desktopWindowTarget != null)
                {
                    desktopWindowTarget.Root = null;
                    desktopWindowTarget.Dispose();
                    desktopWindowTarget = null;
                }
            }
        }

        private void InitializeLayoutRootShadow()
        {
            backgroundShadow = compositor.CreateDropShadow();
            backgroundShadow.Offset = new Vector3(0, 3, 0);
            backgroundShadow.BlurRadius = 12;
            backgroundShadow.Color = Windows.UI.Color.FromArgb(255, 0, 0, 0);
            backgroundShadow.Opacity = 0.8f;
            backgroundShadow.SourcePolicy = CompositionDropShadowSourcePolicy.InheritFromVisualContent;

            backgroundShadowClipGeometry = compositor.CreatePathGeometry();
            backgroundShadowClip = compositor.CreateGeometricClip(backgroundShadowClipGeometry);

            backgroundShadowHostGeometry = compositor.CreatePathGeometry();
            backgroundShadowHostSurfaceShape = compositor.CreateSpriteShape(backgroundShadowHostGeometry);
            backgroundShadowHostSurfaceShape.FillBrush = compositor.CreateColorBrush(Windows.UI.Color.FromArgb(255, 255, 255, 255));

            backgroundShadowHostSurfaceVisual = compositor.CreateShapeVisual();
            backgroundShadowHostSurfaceVisual.Shapes.Add(backgroundShadowHostSurfaceShape);

            backgroundShadowHostSurface = compositor.CreateVisualSurface();
            backgroundShadowHostSurface.SourceVisual = backgroundShadowHostSurfaceVisual;

            backgroundShadowHostSurfaceBrush = compositor.CreateSurfaceBrush(backgroundShadowHostSurface);

            backgroundShadowHostSurfaceBrush.Stretch = CompositionStretch.Uniform;

            backgroundShadowHostVisual = compositor.CreateSpriteVisual();
            backgroundShadowHostVisual.Brush = backgroundShadowHostSurfaceBrush;
            backgroundShadowHostVisual.Shadow = backgroundShadow;
            backgroundShadowHostVisual.Clip = backgroundShadowClip;

            UpdateVisualSize();
        }

        private void InitializeAcrylic()
        {
            acrylicHelper = new AcrylicHelper();

            hostBackdropVisual = WindowsCompositionHelper.Compositor.CreateSpriteVisual();
            hostBackdropVisual.Brush = acrylicHelper.Brush;
            hostBackdropVisual.RelativeSizeAdjustment = Vector2.One;

            hostBackdropGeometry = compositor.CreatePathGeometry();

            hostBackdropVisual.Clip = compositor.CreateGeometricClip(hostBackdropGeometry);
        }

        private void UpdateVisualSize()
        {
            if (backgroundShadowHostVisual == null
                || backgroundShadowClipGeometry == null
                || backgroundShadowHostGeometry == null
                || hostBackdropGeometry == null
                || window == null) return;

            var dpi = window.GetDpiForWindow();
            var pixelSize = window.GetAppWindow().Size;

            var width = pixelSize.Width * 96 / dpi - Padding.Left - Padding.Right;
            var height = (pixelSize.Height) * 96 / dpi - Padding.Top - Padding.Bottom - 3;

            if (width <= 0 || height <= 0)
            {
                backgroundShadowHostVisual.IsVisible = false;
                return;
            }

            var size = new Vector2((float)width, (float)height);
            var offset = new Vector3((float)Padding.Left, (float)Padding.Top, 0);
            var radius = (float)CornerRadius;

            blurAndShadowContainer.Offset = offset;
            blurAndShadowContainer.Size = size;
            backgroundShadowHostVisual.Size = size;
            backgroundShadowHostVisual.IsVisible = true;

            backdropSurface.SourceSize = size;

            if (backgroundShadowHostSurface != null && backgroundShadowHostSurfaceVisual != null)
            {
                backgroundShadowHostSurface.SourceSize = size;
                backgroundShadowHostSurfaceVisual.Size = size;
            }

            if (width <= 1 || height <= 1)
            {
                backgroundShadowHostGeometry.Path = null;
                backgroundShadowClipGeometry.Path = null;
                hostBackdropGeometry.Path = null;
            }
            else
            {
                using (var geometry1 = CanvasGeometry.CreateRoundedRectangle(null, new Windows.Foundation.Rect(-MaxBlurRadius, -MaxBlurRadius, width + MaxBlurRadius * 2, height + MaxBlurRadius * 2), MaxBlurRadius, MaxBlurRadius))
                using (var geometry2 = CanvasGeometry.CreateRoundedRectangle(null, new Windows.Foundation.Rect(0, 0, width, height), radius, radius))
                using (var geometry3 = geometry1.CombineWith(geometry2, Matrix3x2.Identity, CanvasGeometryCombine.Exclude))
                {
                    backgroundShadowClipGeometry.Path = new CompositionPath(geometry3);
                }

                using (var geometry1 = CanvasGeometry.CreateRoundedRectangle(null, new Windows.Foundation.Rect(1, 1, width - 2, height - 2), radius, radius))
                {
                    backgroundShadowHostGeometry.Path = new CompositionPath(geometry1);
                }

                using (var geometry1 = CanvasGeometry.CreateRoundedRectangle(null, new Windows.Foundation.Rect(0, 0, width, height), radius, radius))
                {
                    hostBackdropGeometry.Path = new CompositionPath(geometry1);
                }
            }
        }


        #region Update Size

        [System.Diagnostics.DebuggerNonUserCode]
        private void WindowManager_WindowMessageReceived(object? sender, WinUIEx.Messaging.WindowMessageEventArgs e)
        {
            if (window != null)
            {
                if (e.Message.MessageId == (uint)User32.WindowMessage.WM_DPICHANGED)
                {
                    window.DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Normal, UpdateRootVisualScale);
                }
                else if (e.Message.MessageId == (uint)User32.WindowMessage.WM_SIZE
                    || e.Message.MessageId == (uint)User32.WindowMessage.WM_WINDOWPOSCHANGED)
                {
                    window.DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Normal, UpdateRootVisualSize);
                }
            }
        }

        private void UpdateRootVisualScale()
        {
            if (window != null)
            {
                var dpi = window.GetDpiForWindow();
                var scale = dpi / 96f;
                rootVisual.Scale = new Vector3(scale, scale, 1);

                if (acrylicHelper != null)
                {
                    acrylicHelper.NoiseScaleRatio = scale;
                }
            }
        }

        private void UpdateRootVisualSize()
        {
            if (window != null)
            {
                var size = window.GetAppWindow().Size;
                var dpi = window.GetDpiForWindow();

                var width = size.Width * 96f / dpi;
                var height = size.Height * 96f / dpi;
                rootVisual.Size = new Vector2(width, height);

                UpdateVisualSize();
            }
        }

        #endregion Update Size

        public static DesktopWindowTarget CreateDesktopWindowTarget(nint windowHandle)
        {
            var compositor = WindowsCompositionHelper.Compositor;

            var interop = compositor.As<WindowsCompositionHelper.ICompositorDesktopInterop>();
            interop.CreateDesktopWindowTarget(windowHandle, false, out var raw);
            var target = DesktopWindowTarget.FromAbi(raw);

            return target;
        }

        public void Dispose()
        {
            if (!disposeValue)
            {
                disposeValue = true;

                Window = null;

                rootVisual.Dispose();
                rootVisual = null!;

                acrylicHelper?.Dispose();
                acrylicHelper = null!;
            }
        }
    }
}
