using System;
using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace HotLyric.Win32.Controls
{
    [ContentProperty("Text")]
    public class ProgressTextBlock : FrameworkElement
    {
        private void UpdatePen()
        {
            _Pen1 = new Pen(Stroke1, ActualStrokeThickness1)
            {
                DashCap = PenLineCap.Round,
                EndLineCap = PenLineCap.Round,
                LineJoin = PenLineJoin.Round,
                StartLineCap = PenLineCap.Round
            };

            _Pen2 = new Pen(Stroke2, ActualStrokeThickness2)
            {
                DashCap = PenLineCap.Round,
                EndLineCap = PenLineCap.Round,
                LineJoin = PenLineJoin.Round,
                StartLineCap = PenLineCap.Round
            };

            if (StrokePosition == StrokePosition.Outside || StrokePosition == StrokePosition.Inside)
            {
                _Pen1.Thickness = ActualStrokeThickness1 * 2;
                _Pen2.Thickness = ActualStrokeThickness2 * 2;
            }

            _Pen1.Freeze();
            _Pen2.Freeze();

            InvalidateVisual();
        }

        private double ActualStrokeThickness1
        {
            get
            {
                var value = StrokeThickness1;
                if (double.IsNaN(value))
                {
                    value = Math.Max(0, FontSize / 20);
                }
                return value;
            }
        }

        private double ActualStrokeThickness2
        {
            get
            {
                var value = StrokeThickness2;
                if (double.IsNaN(value))
                {
                    value = Math.Max(0, FontSize / 20);
                }
                return value;
            }
        }

        public static readonly DependencyProperty Fill1Property = DependencyProperty.Register(
          "Fill1",
          typeof(Brush),
          typeof(ProgressTextBlock),
          new FrameworkPropertyMetadata(Brushes.Black, FrameworkPropertyMetadataOptions.AffectsRender, OnFormattedTextUpdated));

        public static readonly DependencyProperty Fill2Property = DependencyProperty.Register(
          "Fill2",
          typeof(Brush),
          typeof(ProgressTextBlock),
          new FrameworkPropertyMetadata(Brushes.Black, FrameworkPropertyMetadataOptions.AffectsRender, OnFormattedTextUpdated));

        public static readonly DependencyProperty Stroke1Property = DependencyProperty.Register(
          "Stroke1",
          typeof(Brush),
          typeof(ProgressTextBlock),
          new FrameworkPropertyMetadata(Brushes.Black, FrameworkPropertyMetadataOptions.AffectsRender, OnFormattedTextUpdated));

        public static readonly DependencyProperty Stroke2Property = DependencyProperty.Register(
          "Stroke2",
          typeof(Brush),
          typeof(ProgressTextBlock),
          new FrameworkPropertyMetadata(Brushes.Black, FrameworkPropertyMetadataOptions.AffectsRender, OnFormattedTextUpdated));

        public static readonly DependencyProperty StrokeThickness1Property = DependencyProperty.Register(
          "StrokeThickness1",
          typeof(double),
          typeof(ProgressTextBlock),
          new FrameworkPropertyMetadata(double.NaN, FrameworkPropertyMetadataOptions.AffectsRender, OnFormattedTextUpdated));

        public static readonly DependencyProperty StrokeThickness2Property = DependencyProperty.Register(
          "StrokeThickness2",
          typeof(double),
          typeof(ProgressTextBlock),
          new FrameworkPropertyMetadata(double.NaN, FrameworkPropertyMetadataOptions.AffectsRender, OnFormattedTextUpdated));

        public static readonly DependencyProperty FontFamilyProperty = TextElement.FontFamilyProperty.AddOwner(
          typeof(ProgressTextBlock),
          new FrameworkPropertyMetadata(OnFormattedTextUpdated));

        public static readonly DependencyProperty FontSizeProperty = TextElement.FontSizeProperty.AddOwner(
          typeof(ProgressTextBlock),
          new FrameworkPropertyMetadata(OnFormattedTextUpdated));

        public static readonly DependencyProperty FontStretchProperty = TextElement.FontStretchProperty.AddOwner(
          typeof(ProgressTextBlock),
          new FrameworkPropertyMetadata(OnFormattedTextUpdated));

        public static readonly DependencyProperty FontStyleProperty = TextElement.FontStyleProperty.AddOwner(
          typeof(ProgressTextBlock),
          new FrameworkPropertyMetadata(OnFormattedTextUpdated));

        public static readonly DependencyProperty FontWeightProperty = TextElement.FontWeightProperty.AddOwner(
          typeof(ProgressTextBlock),
          new FrameworkPropertyMetadata(OnFormattedTextUpdated));

        public static readonly DependencyProperty TextProperty = DependencyProperty.Register(
          "Text",
          typeof(string),
          typeof(ProgressTextBlock),
          new FrameworkPropertyMetadata(OnFormattedTextInvalidated));

        public static readonly DependencyProperty TextAlignmentProperty = DependencyProperty.Register(
          "TextAlignment",
          typeof(TextAlignment),
          typeof(ProgressTextBlock),
          new FrameworkPropertyMetadata(OnFormattedTextUpdated));

        public static readonly DependencyProperty TextDecorationsProperty = DependencyProperty.Register(
          "TextDecorations",
          typeof(TextDecorationCollection),
          typeof(ProgressTextBlock),
          new FrameworkPropertyMetadata(OnFormattedTextUpdated));

        public static readonly DependencyProperty TextTrimmingProperty = DependencyProperty.Register(
          "TextTrimming",
          typeof(TextTrimming),
          typeof(ProgressTextBlock),
          new FrameworkPropertyMetadata(OnFormattedTextUpdated));

        public static readonly DependencyProperty TextWrappingProperty = DependencyProperty.Register(
          "TextWrapping",
          typeof(TextWrapping),
          typeof(ProgressTextBlock),
          new FrameworkPropertyMetadata(TextWrapping.NoWrap, OnFormattedTextUpdated));

        public static readonly DependencyProperty ProgressProperty = DependencyProperty.Register(
            "Progress",
            typeof(double),
            typeof(ProgressTextBlock),
            new FrameworkPropertyMetadata(0d, FrameworkPropertyMetadataOptions.AffectsRender));

        private FormattedText? _FormattedText;
        private Geometry? _Text1Geometry;
        private Geometry? _Text2Geometry;
        private Pen? _Pen1;
        private Pen? _Pen2;
        private Geometry? _clip1Geometry;
        private Geometry? _clip2Geometry;
        private RenderTargetBitmap? _cachedBitmap1;
        private RenderTargetBitmap? _cachedBitmap2;
        private DrawingVisual? _drawingVisual;
        private Size lastRenderSize;
        private Geometry? _renderTextClipGeometry1;
        private Geometry? _renderTextClipGeometry2;
        private ScaleTransform _renderTextClipScale1 = new ScaleTransform(1, 1);
        private ScaleTransform _renderTextClipScale2 = new ScaleTransform(1, 1);

        private StrokePosition StrokePosition => StrokePosition.Outside;

        public Brush? Fill1
        {
            get { return (Brush?)GetValue(Fill1Property); }
            set { SetValue(Fill1Property, value); }
        }

        public Brush? Fill2
        {
            get { return (Brush?)GetValue(Fill2Property); }
            set { SetValue(Fill2Property, value); }
        }

        public FontFamily? FontFamily
        {
            get { return (FontFamily?)GetValue(FontFamilyProperty); }
            set { SetValue(FontFamilyProperty, value); }
        }

        [TypeConverter(typeof(FontSizeConverter))]
        public double FontSize
        {
            get { return (double)GetValue(FontSizeProperty); }
            set { SetValue(FontSizeProperty, value); }
        }

        public FontStretch FontStretch
        {
            get { return (FontStretch)GetValue(FontStretchProperty); }
            set { SetValue(FontStretchProperty, value); }
        }

        public FontStyle FontStyle
        {
            get { return (FontStyle)GetValue(FontStyleProperty); }
            set { SetValue(FontStyleProperty, value); }
        }

        public FontWeight FontWeight
        {
            get { return (FontWeight)GetValue(FontWeightProperty); }
            set { SetValue(FontWeightProperty, value); }
        }

        public Brush? Stroke1
        {
            get { return (Brush?)GetValue(Stroke1Property); }
            set { SetValue(Stroke1Property, value); }
        }

        public Brush? Stroke2
        {
            get { return (Brush?)GetValue(Stroke2Property); }
            set { SetValue(Stroke2Property, value); }
        }

        public double StrokeThickness1
        {
            get { return (double)GetValue(StrokeThickness1Property); }
            set { SetValue(StrokeThickness1Property, value); }
        }

        public double StrokeThickness2
        {
            get { return (double)GetValue(StrokeThickness2Property); }
            set { SetValue(StrokeThickness2Property, value); }
        }

        public string Text
        {
            get { return (string)GetValue(TextProperty); }
            set { SetValue(TextProperty, value); }
        }

        public TextAlignment TextAlignment
        {
            get { return (TextAlignment)GetValue(TextAlignmentProperty); }
            set { SetValue(TextAlignmentProperty, value); }
        }

        public TextDecorationCollection TextDecorations
        {
            get { return (TextDecorationCollection)GetValue(TextDecorationsProperty); }
            set { SetValue(TextDecorationsProperty, value); }
        }

        public TextTrimming TextTrimming
        {
            get { return (TextTrimming)GetValue(TextTrimmingProperty); }
            set { SetValue(TextTrimmingProperty, value); }
        }

        public TextWrapping TextWrapping
        {
            get { return (TextWrapping)GetValue(TextWrappingProperty); }
            set { SetValue(TextWrappingProperty, value); }
        }

        public double Progress
        {
            get { return (double)GetValue(ProgressProperty); }
            set { SetValue(ProgressProperty, value); }
        }

        public ProgressTextBlock()
        {
            UpdatePen();
            TextDecorations = new TextDecorationCollection();
        }

        protected override void OnDpiChanged(DpiScale oldDpi, DpiScale newDpi)
        {
            base.OnDpiChanged(oldDpi, newDpi);

            _FormattedText = null;
            InvalidateMeasure();
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            EnsureGeometry();

            var progress = Math.Clamp(Progress, 0, 1);

            if (progress < 1 && _cachedBitmap1 != null)
            {
                // 显示普通文字
                _renderTextClipScale1.ScaleX = (1 - progress);
                drawingContext.PushClip(_renderTextClipGeometry1);
                drawingContext.DrawImage(_cachedBitmap1, new Rect(0, 0, _cachedBitmap1.Width, _cachedBitmap1.Height));
                drawingContext.Pop();
            }
            if (progress > 0 && _cachedBitmap2 != null)
            {
                // 显示卡拉OK文字
                _renderTextClipScale2.ScaleX = progress;
                drawingContext.PushClip(_renderTextClipGeometry2);
                drawingContext.DrawImage(_cachedBitmap2, new Rect(0, 0, _cachedBitmap2.Width, _cachedBitmap2.Height));
                drawingContext.Pop();
            }
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            EnsureFormattedText();

            // constrain the formatted text according to the available size

            double w = availableSize.Width;
            double h = availableSize.Height;

            // the Math.Min call is important - without this constraint (which seems arbitrary, but is the maximum allowable text width), things blow up when availableSize is infinite in both directions
            // the Math.Max call is to ensure we don't hit zero, which will cause MaxTextHeight to throw
            _FormattedText!.MaxTextWidth = Math.Min(3579139, w);
            _FormattedText.MaxTextHeight = Math.Max(0.0001d, h);

            var strokeThickness = Math.Max(ActualStrokeThickness1, ActualStrokeThickness2);

            lastRenderSize = default;

            // return the desired size
            return new Size(
                Math.Ceiling(_FormattedText.Width + 2 * strokeThickness),
                Math.Ceiling(_FormattedText.Height + 2 * strokeThickness));
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            EnsureFormattedText();

            // update the formatted text with the final size
            _FormattedText!.MaxTextWidth = finalSize.Width;
            _FormattedText.MaxTextHeight = Math.Max(0.0001d, finalSize.Height);

            // need to re-generate the geometry now that the dimensions have changed

            if (lastRenderSize != finalSize)
            {
                _Text1Geometry = null;
                _Text2Geometry = null;
                lastRenderSize = finalSize;
            }

            UpdatePen();

            return finalSize;
        }

        private static void OnFormattedTextInvalidated(DependencyObject dependencyObject,
          DependencyPropertyChangedEventArgs e)
        {
            var ProgressTextBlock = (ProgressTextBlock)dependencyObject;
            ProgressTextBlock._FormattedText = null;
            ProgressTextBlock._Text1Geometry = null;
            ProgressTextBlock._Text2Geometry = null;

            ProgressTextBlock.InvalidateMeasure();
            ProgressTextBlock.InvalidateVisual();
        }

        private static void OnFormattedTextUpdated(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
        {
            var ProgressTextBlock = (ProgressTextBlock)dependencyObject;
            ProgressTextBlock.UpdateFormattedText();
            ProgressTextBlock._Text1Geometry = null;
            ProgressTextBlock._Text2Geometry = null;

            ProgressTextBlock.InvalidateMeasure();
            ProgressTextBlock.InvalidateVisual();
        }

        private void EnsureFormattedText()
        {
            if (_FormattedText != null)
            {
                return;
            }

            var dpi = VisualTreeHelper.GetDpi(this);

            _FormattedText = new FormattedText(
              Text ?? "",
              CultureInfo.CurrentUICulture,
              FlowDirection,
              new Typeface(FontFamily, FontStyle, FontWeight, FontStretch),
              FontSize,
              Brushes.Black,
              dpi.PixelsPerDip);

            UpdateFormattedText();
        }

        private void UpdateFormattedText()
        {
            if (_FormattedText == null)
            {
                return;
            }

            _FormattedText.MaxLineCount = TextWrapping == TextWrapping.NoWrap ? 1 : int.MaxValue;
            _FormattedText.TextAlignment = TextAlignment;
            _FormattedText.Trimming = TextTrimming;

            _FormattedText.SetFontSize(FontSize);
            _FormattedText.SetFontStyle(FontStyle);
            _FormattedText.SetFontWeight(FontWeight);
            _FormattedText.SetFontFamily(FontFamily);
            _FormattedText.SetFontStretch(FontStretch);
            _FormattedText.SetTextDecorations(TextDecorations);
        }

        private void EnsureGeometry()
        {
            if (_Text1Geometry != null || _Text2Geometry != null)
            {
                return;
            }

            EnsureFormattedText();

            var strokeThickness = Math.Max(ActualStrokeThickness1, ActualStrokeThickness2);

            _Text1Geometry = BuildGeometry(_FormattedText, strokeThickness);
            _Text2Geometry = BuildGeometry(_FormattedText, strokeThickness);

            if (StrokePosition == StrokePosition.Outside)
            {
                _clip1Geometry = BuildClipGeometry(_Text1Geometry, strokeThickness);
                _clip2Geometry = BuildClipGeometry(_Text2Geometry, strokeThickness);
            }

            _cachedBitmap1 = CreateCachedBitmap(_cachedBitmap1);
            _cachedBitmap2 = CreateCachedBitmap(_cachedBitmap2);

            _renderTextClipGeometry1 = new RectangleGeometry(new Rect(0, 0, ActualWidth, ActualHeight), 0, 0, _renderTextClipScale1);
            _renderTextClipGeometry2 = new RectangleGeometry(new Rect(0, 0, ActualWidth, ActualHeight), 0, 0, _renderTextClipScale2);

            _renderTextClipScale1.CenterX = ActualWidth;

            DrawCachedBitmap(_Text1Geometry, _clip1Geometry, _cachedBitmap1, Fill1, _Pen1, strokeThickness);
            DrawCachedBitmap(_Text2Geometry, _clip2Geometry, _cachedBitmap2, Fill2, _Pen2, strokeThickness);
        }

        private Geometry? BuildGeometry(FormattedText? formattedText, double strokeThickness)
        {
            if (formattedText == null) return null;

            var geometry = formattedText.BuildGeometry(new Point(strokeThickness, strokeThickness));
            geometry.Freeze();
            return geometry;
        }

        private Geometry? BuildClipGeometry(Geometry? textGeometry, double strokeThickness)
        {
            if (textGeometry == null) return null;

            var boundsGeometry = new RectangleGeometry(new Rect(-(2 * strokeThickness), -(2 * strokeThickness), ActualWidth + (4 * strokeThickness), ActualHeight + (4 * strokeThickness)));
            var geometry = Geometry.Combine(boundsGeometry, textGeometry, GeometryCombineMode.Exclude, null);
            geometry.Freeze();

            return geometry;
        }

        private RenderTargetBitmap? CreateCachedBitmap(RenderTargetBitmap? oldBitmap)
        {
            var bitmap = oldBitmap;

            var dpi = VisualTreeHelper.GetDpi(this);

            var width = ActualWidth;
            var height = ActualHeight;

            if (width > 0 && height > 0 && (oldBitmap == null || Math.Abs(oldBitmap.Width - width) > 0.01 || Math.Abs(oldBitmap.Height - height) > 0.01))
            {
                GC.Collect();

                bitmap = new RenderTargetBitmap(
                    (int)Math.Ceiling(width * dpi.PixelsPerDip),
                    (int)Math.Ceiling(height * dpi.PixelsPerDip),
                    dpi.PixelsPerInchX,
                    dpi.PixelsPerInchY,
                    PixelFormats.Pbgra32);
            }

            return bitmap;
        }

        private void DrawCachedBitmap(Geometry? textGeometry, Geometry? clipGeometry, RenderTargetBitmap? cachedBitmap, Brush? fill, Pen? stroke, double strokeThickness)
        {
            if (_FormattedText == null || textGeometry == null || cachedBitmap == null) return;

            if (_drawingVisual == null)
            {
                _drawingVisual = new DrawingVisual();
                RenderOptions.SetBitmapScalingMode(_drawingVisual, BitmapScalingMode.HighQuality);
                RenderOptions.SetEdgeMode(_drawingVisual, EdgeMode.Aliased);
            }

            lock (_drawingVisual)
            {
                using (var drawingContext = _drawingVisual.RenderOpen())
                {
                    bool flag = false;

                    if (StrokePosition == StrokePosition.Outside && clipGeometry != null)
                    {
                        flag = true;
                        drawingContext.PushClip(clipGeometry);
                    }
                    else if (StrokePosition == StrokePosition.Inside)
                    {
                        flag = true;
                        drawingContext.PushClip(textGeometry);
                    }

                    drawingContext.DrawGeometry(null, stroke, textGeometry);

                    if (flag)
                    {
                        drawingContext.Pop();
                    }

                    if (fill != null)
                    {
                        // 使用形状绘制文本
                        drawingContext.DrawGeometry(fill, null, textGeometry);

                        // 直接绘制文本
                        //_FormattedText.SetForegroundBrush(fill);
                        //drawingContext.DrawText(_FormattedText, new Point(strokeThickness, strokeThickness));
                        //_FormattedText.SetForegroundBrush(Brushes.Black);
                    }
                }

                cachedBitmap.Render(_drawingVisual);
            }
        }
    }
}
