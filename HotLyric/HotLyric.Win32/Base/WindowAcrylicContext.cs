using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vanara.PInvoke;
using Windows.UI.Composition.Desktop;
using WinRT;
using System.Reflection.Metadata;
using Windows.UI.Composition;
using System.Numerics;
using Microsoft.Graphics.Canvas.Geometry;
using HotLyric.Win32.Utils;
using HotLyric.Win32.Base.BackgroundHelpers;
using Microsoft.UI.Xaml;
using BlueFire.Toolkit.WinUI3.WindowBase;
using Microsoft.UI;
using BlueFire.Toolkit.WinUI3.Extensions;

namespace HotLyric.Win32.Base
{
    public class WindowAcrylicContext : IDisposable
    {
        private static readonly bool IsPixelSnappingEnabledSupported = Environment.OSVersion.Version >= new Version(10, 0, 20384, 0);

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

        private CompositionPathGeometry? borderGeometry;
        private CompositionColorBrush? borderBrush;
        private CompositionSpriteShape? borderShape;
        private ShapeVisual? borderVisual;

        private Thickness margin = new Thickness(10);
        private double cornerRadius = 8;

        private AcrylicHelper? acrylicHelper;

        internal const string scaleAnimationSize = "12f";
        internal static readonly TimeSpan opacityAnimationDuration = TimeSpan.FromSeconds(0.27);
        private ExpressionAnimation centerPointBind;
        private ExpressionAnimation scaleBind;

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
            InitializeBorder();

            UpdateVisualSize();

            blurAndShadowContainer.Children.InsertAtTop(backgroundShadowHostVisual);
            blurAndShadowContainer.Children.InsertAtTop(hostBackdropVisual);
            blurAndShadowContainer.Children.InsertAtTop(borderVisual);

            rootVisual.Children.InsertAtTop(blurAndShadowContainer);

            centerPointBind = compositor.CreateExpressionAnimation("Vector3(this.Target.Size.X / 2, this.Target.Size.Y / 2, 0)");
            blurAndShadowContainer.StartAnimation("CenterPoint", centerPointBind);

            scaleBind = compositor.CreateExpressionAnimation($"Vector3(({scaleAnimationSize} / this.Target.Size.X) * (1 - this.Target.Opacity) + 1, ({scaleAnimationSize} / this.Target.Size.Y) * (1 - this.Target.Opacity) + 1, 1)");
            blurAndShadowContainer.StartAnimation("Scale", scaleBind);

            var imp = compositor.CreateImplicitAnimationCollection();
            var opacityAn = compositor.CreateScalarKeyFrameAnimation();
            opacityAn.InsertExpressionKeyFrame(1, "this.FinalValue");
            opacityAn.Duration = opacityAnimationDuration;
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

        public Thickness Margin
        {
            get => margin;
            set
            {
                if (margin != value)
                {
                    margin = value;
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
            set
            {
                var v = (float)value;
                if (blurAndShadowContainer.Opacity != v)
                {
                    if (blurAndShadowContainer.Opacity == 0)
                    {
                        acrylicHelper?.FlushBrush();
                    }
                    blurAndShadowContainer.Opacity = v;
                }
            }
        }

        public bool Visible
        {
            get => rootVisual.IsVisible;
            set => rootVisual.IsVisible = value;
        }

        public Windows.UI.Color BorderColor
        {
            get => borderBrush!.Color;
            set => borderBrush!.Color = value;
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
                desktopWindowTarget = CreateDesktopWindowTarget(window.GetWindowHandle());

                windowManager = WindowManager.Get(window);
                if (windowManager != null)
                {
                    windowManager.WindowMessageReceived += WindowManager_WindowMessageReceived;
                }

                desktopWindowTarget.Root = rootVisual;

                UpdateRootVisualScale();

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
        }

        private void InitializeAcrylic()
        {
            acrylicHelper = new AcrylicHelper();

            hostBackdropVisual = WindowsCompositionHelper.Compositor.CreateSpriteVisual();
            hostBackdropVisual.RelativeSizeAdjustment = Vector2.One;

            var acrylicVisual = WindowsCompositionHelper.Compositor.CreateSpriteVisual();
            acrylicVisual.Brush = acrylicHelper.Brush;
            acrylicVisual.RelativeSizeAdjustment = Vector2.One;
            hostBackdropVisual.Children.InsertAtTop(acrylicVisual);

            if (AcrylicHelper.IsHostBackdropBrushSupported)
            {
                var fixVisual = WindowsCompositionHelper.Compositor.CreateSpriteVisual();
                fixVisual.Brush = WindowsCompositionHelper.Compositor.CreateColorBrush(Windows.UI.Color.FromArgb(255, 0, 0, 0)); ;
                fixVisual.RelativeSizeAdjustment = Vector2.One;
                hostBackdropVisual.Children.InsertAtBottom(fixVisual);
            }

            hostBackdropGeometry = compositor.CreatePathGeometry();

            hostBackdropVisual.Clip = compositor.CreateGeometricClip(hostBackdropGeometry);
        }

        private void InitializeBorder()
        {
            borderBrush = compositor.CreateColorBrush(Windows.UI.Color.FromArgb(0, 255, 255, 255));

            borderGeometry = compositor.CreatePathGeometry();

            borderShape = compositor.CreateSpriteShape(borderGeometry);
            borderShape.StrokeBrush = borderBrush;
            borderShape.StrokeThickness = 0;

            borderVisual = compositor.CreateShapeVisual();
            borderVisual.RelativeSizeAdjustment = Vector2.One;
            borderVisual.Shapes.Add(borderShape);

            if (IsPixelSnappingEnabledSupported)
            {
                borderVisual.IsPixelSnappingEnabled = true;
            }
        }

        private void UpdateVisualSize()
        {
            if (backgroundShadowHostVisual == null
                || backgroundShadowClipGeometry == null
                || backgroundShadowHostGeometry == null
                || hostBackdropGeometry == null
                || borderGeometry == null
                || borderShape == null
                || window == null) return;

            var dpi = window.GetDpiForWindow();
            var pixelSize = window.AppWindow.Size;

            var width = pixelSize.Width * 96 / dpi - Margin.Left - Margin.Right;
            var height = pixelSize.Height * 96 / dpi - Margin.Top - Margin.Bottom;

            if (width <= 0 || height <= 0)
            {
                backgroundShadowHostVisual.IsVisible = false;
                return;
            }

            var size = new Vector2((float)width, (float)height);
            var offset = new Vector3((float)Margin.Left, (float)Margin.Top, 0);

            var radius = (float)(CornerRadius);

            blurAndShadowContainer.Offset = offset;
            blurAndShadowContainer.Size = size;
            backgroundShadowHostVisual.Size = size;
            backgroundShadowHostVisual.IsVisible = true;

            backdropSurface.SourceSize = size;

            var borderThickness = 1f;

            if (IsPixelSnappingEnabledSupported)
            {
                borderShape.StrokeThickness = borderThickness;
            }
            else
            {
                var borderThicknessPixel = (int)Math.Round(borderThickness * dpi / 96);

                borderThickness = (float)(borderThicknessPixel * 96d / dpi);
                borderShape.StrokeThickness = borderThickness;
            }

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
                borderGeometry = null;
            }
            else
            {
                using (var geometry1 = CanvasGeometry.CreateRoundedRectangle(null, new Windows.Foundation.Rect(-MaxBlurRadius, -MaxBlurRadius, width + MaxBlurRadius * 2, height + MaxBlurRadius * 2), MaxBlurRadius, MaxBlurRadius))
                using (var geometry2 = CanvasGeometry.CreateRoundedRectangle(null, new Windows.Foundation.Rect(borderThickness, borderThickness, width - borderThickness * 2, height - borderThickness * 2), radius, radius))
                using (var geometry3 = geometry1.CombineWith(geometry2, Matrix3x2.Identity, CanvasGeometryCombine.Exclude))
                {
                    backgroundShadowClipGeometry.Path = new CompositionPath(geometry3);
                }
                using (var geometry = CanvasGeometry.CreateRoundedRectangle(null, new Windows.Foundation.Rect(borderThickness + 0.5, borderThickness + 0.5, width - 2 * (borderThickness + 0.5), height - 2 * (borderThickness + 0.5)), radius, radius))
                {
                    backgroundShadowHostGeometry.Path = new CompositionPath(geometry);
                }

                using (var geometry = CanvasGeometry.CreateRoundedRectangle(null, new Windows.Foundation.Rect(0, 0, width, height), radius, radius))
                {
                    hostBackdropGeometry.Path = new CompositionPath(geometry);
                }

                using (var geometry = CanvasGeometry.CreateRoundedRectangle(null, new Windows.Foundation.Rect(borderThickness / 2, borderThickness / 2, width - borderThickness, height - borderThickness), radius - borderThickness / 2, radius - borderThickness / 2))
                {
                    // 使用CanvasGeometry.CreateRoundedRectangle创建圆角矩形Geometry
                    // 使用Stroke呈现时，着色是以Geometry的线条为中线向两侧扩展着色
                    // CornerRadius此时是中线的角半径
                    // 如果需要设置外侧角半径为 radius 时，中线半径应为 radius - borderThickness / 2
                    // 此时内测半径为 radius - borderThickness

                    borderGeometry.Path = new CompositionPath(geometry);
                }

            }
        }


        #region Update Size

        [System.Diagnostics.DebuggerNonUserCode]
        private unsafe void WindowManager_WindowMessageReceived(object? sender, WindowMessageReceivedEventArgs e)
        {
            if (window != null)
            {
                if (e.MessageId == (uint)User32.WindowMessage.WM_DPICHANGED)
                {
                    window.DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Normal, UpdateRootVisualScale);
                }
                else if (e.MessageId == (uint)User32.WindowMessage.WM_SIZE)
                {
                    window.DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Normal, UpdateRootVisualSize);
                }
                else if (e.MessageId == (uint)User32.WindowMessage.WM_WINDOWPOSCHANGED)
                {
                    var wndpos = (User32.WINDOWPOS*)e.LParam.ToPointer();
                    var flag = wndpos->flags;

                    if ((flag & (User32.SetWindowPosFlags.SWP_NOSIZE)) == 0)
                    {
                        window.DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Normal, UpdateRootVisualSize);
                    }
                }
                else if (e.MessageId == (uint)User32.WindowMessage.WM_SHOWWINDOW)
                {
                    if (window.Visible)
                    {
                        window.DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Normal, UpdateRootVisualScale);
                    }
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
            }

            UpdateRootVisualSize();
        }

        private void UpdateRootVisualSize()
        {
            if (window != null)
            {
                var size = window.AppWindow.Size;
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

            var interop = compositor.As<NativeMethods.ICompositorDesktopInterop>();
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
