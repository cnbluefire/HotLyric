using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Markup;

namespace HotLyric.Win32.Controls
{
    [ContentProperty(Name = "TextBlock")]
    internal class AutoScrollText : Control
    {
        private long visibilityPropertyChangedToken;
        private Border? LayoutRoot;
        private StackPanel? Container;
        private ContentPresenter? ContentPresenter;

        public AutoScrollText()
        {
            this.DefaultStyleKey = typeof(AutoScrollText);

            visibilityPropertyChangedToken = RegisterPropertyChangedCallback(VisibilityProperty, OnVisibilityPropertyChanged);
            this.SizeChanged += AutoScrollText_SizeChanged;
        }

        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            if (ContentPresenter != null)
            {
                ContentPresenter.SizeChanged -= ContentPresenter_SizeChanged;
            }

            LayoutRoot = GetTemplateChild(nameof(LayoutRoot)) as Border;
            Container = GetTemplateChild(nameof(Container)) as StackPanel;
            ContentPresenter = GetTemplateChild(nameof(ContentPresenter)) as ContentPresenter;

            SetupResources();

            if (ContentPresenter != null)
            {
                ContentPresenter.SizeChanged += ContentPresenter_SizeChanged;
            }
        }

        public TextBlock TextBlock
        {
            get { return (TextBlock)GetValue(TextBlockProperty); }
            set { SetValue(TextBlockProperty, value); }
        }

        public static readonly DependencyProperty TextBlockProperty =
            DependencyProperty.Register("TextBlock", typeof(TextBlock), typeof(AutoScrollText), new PropertyMetadata(null, (s, a) =>
            {
                if (s is AutoScrollText sender && !Equals(a.NewValue, a.OldValue))
                {
                    sender.UpdateScrollState();
                }
            }));



        public double PixelsMovedPreSeconds
        {
            get { return (double)GetValue(PixelsMovedPreSecondsProperty); }
            set { SetValue(PixelsMovedPreSecondsProperty, value); }
        }

        // Using a DependencyProperty as the backing store for PixelsMovedPreSeconds.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty PixelsMovedPreSecondsProperty =
            DependencyProperty.Register("PixelsMovedPreSeconds", typeof(double), typeof(AutoScrollText), new PropertyMetadata(20d, (s, a) =>
            {
                if (s is AutoScrollText sender && !Equals(a.NewValue, a.OldValue))
                {
                    sender.UpdateScrollState();
                }
            }));



        public bool Paused
        {
            get { return (bool)GetValue(PausedProperty); }
            set { SetValue(PausedProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Paused.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty PausedProperty =
            DependencyProperty.Register("Paused", typeof(bool), typeof(AutoScrollText), new PropertyMetadata(false, (s, a) =>
            {
                if (s is AutoScrollText sender && !Equals(a.NewValue, a.OldValue))
                {
                    var controller1 = sender.visual1?.TryGetAnimationController("Offset.X");
                    var controller2 = sender.visual2?.TryGetAnimationController("Offset.X");

                    if (a.NewValue is true)
                    {
                        controller1?.Pause();
                        controller2?.Pause();
                    }
                    else
                    {
                        sender.UpdateScrollState();
                    }
                }
            }));



        private void OnVisibilityPropertyChanged(DependencyObject sender, DependencyProperty dp)
        {
            UpdateScrollState();
        }

        private void ContentPresenter_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateScrollState();
        }

        private void AutoScrollText_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateScrollState();
        }


        private SpriteVisual? visual1;
        private SpriteVisual? visual2;
        private ContainerVisual? scrollVisual;
        private ExpressionAnimation? sizeBind;
        private CompositionVisualSurface? surface;
        private CompositionSurfaceBrush? brush;
        private ScalarKeyFrameAnimation? visual1Animation;
        private ScalarKeyFrameAnimation? visual2Animation;
        private LinearEasingFunction? linearEasingFunc;

        private void SetupResources()
        {
            ElementCompositionPreview.SetElementChildVisual(this, null);

            if (LayoutRoot == null || Container == null || ContentPresenter == null) return;

            var containerVisual = ElementCompositionPreview.GetElementVisual(Container);
            var textVisual = ElementCompositionPreview.GetElementVisual(ContentPresenter);
            var compositor = textVisual.Compositor;

            visual1 = compositor.CreateSpriteVisual();
            visual2 = compositor.CreateSpriteVisual();

            sizeBind = compositor.CreateExpressionAnimation("visual.Size");
            sizeBind.SetReferenceParameter("visual", textVisual);

            visual1.StartAnimation("Size", sizeBind);
            visual2.StartAnimation("Size", sizeBind);

            surface = compositor.CreateVisualSurface();
            surface.SourceVisual = textVisual;

            brush = compositor.CreateSurfaceBrush(surface);
            visual1.Brush = brush;
            visual2.Brush = brush;

            scrollVisual = compositor.CreateContainerVisual();
            scrollVisual.Children.InsertAtTop(visual2);
            scrollVisual.Children.InsertAtTop(visual1);

            scrollVisual.RelativeSizeAdjustment = Vector2.One;
            scrollVisual.Clip = compositor.CreateInsetClip();

            ElementCompositionPreview.SetElementChildVisual(this, scrollVisual);

            containerVisual.Opacity = 0;

            UpdateScrollState();
        }

        private void UpdateScrollState()
        {
            const double TextSpace = 10d;

            if (Visibility == Visibility.Visible && surface != null && ContentPresenter != null)
            {
                surface.SourceSize = new System.Numerics.Vector2(
                    (float)ContentPresenter.ActualWidth,
                    (float)ContentPresenter.ActualHeight);

                if (!Paused
                    && TextBlock != null
                    && scrollVisual != null
                    && ContentPresenter != null
                    && LayoutRoot != null
                    && ContentPresenter.ActualWidth > LayoutRoot.ActualWidth)
                {
                    var compositor = scrollVisual.Compositor;

                    if (linearEasingFunc == null)
                    {
                        linearEasingFunc = compositor.CreateLinearEasingFunction();
                    }

                    var duration = TimeSpan.FromSeconds((ContentPresenter.ActualWidth + TextSpace) / PixelsMovedPreSeconds);

                    visual1Animation = compositor.CreateScalarKeyFrameAnimation();
                    visual1Animation.InsertKeyFrame(0f, 0f, linearEasingFunc);
                    visual1Animation.InsertKeyFrame(1f, -(float)(ContentPresenter.ActualWidth + TextSpace), linearEasingFunc);
                    visual1Animation.IterationBehavior = AnimationIterationBehavior.Forever;
                    visual1Animation.Duration = duration;

                    visual2Animation = compositor.CreateScalarKeyFrameAnimation();
                    visual2Animation.InsertKeyFrame(0f, (float)(ContentPresenter.ActualWidth + TextSpace), linearEasingFunc);
                    visual2Animation.InsertKeyFrame(1f, 0, linearEasingFunc);
                    visual2Animation.IterationBehavior = AnimationIterationBehavior.Forever;
                    visual2Animation.Duration = duration;

                    visual1?.StartAnimation("Offset.X", visual1Animation);
                    visual2?.StartAnimation("Offset.X", visual2Animation);

                    if (visual2 != null)
                    {
                        visual2.IsVisible = true;
                    }
                }
                else
                {
                    if (visual1 != null)
                    {
                        visual1.StopAnimation("Offset.X");
                        visual1.Offset = default;
                    }
                    if (visual2 != null)
                    {
                        visual2.StopAnimation("Offset.X");
                        visual2.IsVisible = false;
                    }

                    visual1Animation = null;
                    visual2Animation = null;
                }
            }
        }
    }
}
