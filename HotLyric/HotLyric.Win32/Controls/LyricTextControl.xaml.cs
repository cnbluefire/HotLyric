using HotLyric.Win32.Utils;
using Kfstorm.LrcParser;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace HotLyric.Win32.Controls
{
    /// <summary>
    /// LyricTextControl.xaml 的交互逻辑
    /// </summary>
    public partial class LyricTextControl : UserControl
    {
        static LyricTextControl()
        {
            IsEmptyPropertyKey = DependencyProperty.RegisterReadOnly("IsEmpty", typeof(bool), typeof(LyricTextControl), new PropertyMetadata(true));
            IsEmptyProperty = IsEmptyPropertyKey.DependencyProperty;

            IsSecondRowVisiblePropertyKey = DependencyProperty.RegisterReadOnly("IsSecondRowVisible", typeof(bool), typeof(LyricTextControl), new PropertyMetadata(false, (s, a) =>
            {
                if (!object.Equals(a.NewValue, a.OldValue) && s is LyricTextControl sender)
                {
                    sender.UpdateAnimationPlaceholder();
                    sender.OnIsSecondRowVisibleChanged();
                }
            }));
            IsSecondRowVisibleProperty = IsSecondRowVisiblePropertyKey.DependencyProperty;
        }

        private LinearGradientBrush maskBrush;
        private Storyboard? curSb;
        private Storyboard? curRowSb;
        private Storyboard? curClipSb;
        private PositionProvider positionProvider;
        private LrcFileWrapper? lrcFileWrapper;
        private bool isTranslationControl;

        public LyricTextControl()
        {
            InitializeComponent();
            positionProvider = new PositionProvider(TimeSpan.FromMilliseconds(400), null);
            positionProvider.PositionChanged += PositionProvider_PositionChanged;
            this.SizeChanged += LyricTextControl_SizeChanged;
            this.IsVisibleChanged += LyricTextControl_IsVisibleChanged;

            maskBrush = new LinearGradientBrush()
            {
                StartPoint = new Point(0, 0),
                EndPoint = new Point(0, 0),
                MappingMode = BrushMappingMode.Absolute,
                GradientStops =
                {
                    new GradientStop(Colors.Transparent, 0),
                    new GradientStop(Colors.Black, 0.4),
                    new GradientStop(Colors.Black, 0.6),
                    new GradientStop(Colors.Transparent, 1),
                }
            };

            UpdateStrokeProperties();
            UpdateAnimationPlaceholder();
        }

        public Brush KaraokeBrush
        {
            get { return (Brush)GetValue(KaraokeBrushProperty); }
            set { SetValue(KaraokeBrushProperty, value); }
        }

        // Using a DependencyProperty as the backing store for KaraokeBrush.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty KaraokeBrushProperty =
            DependencyProperty.Register("KaraokeBrush", typeof(Brush), typeof(LyricTextControl), new PropertyMetadata(new SolidColorBrush(Color.FromArgb(255, 255, 160, 77))));



        public Brush Stroke
        {
            get { return (Brush)GetValue(StrokeProperty); }
            set { SetValue(StrokeProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Stroke.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty StrokeProperty =
            DependencyProperty.Register("Stroke", typeof(Brush), typeof(LyricTextControl), new PropertyMetadata(Brushes.Black, (s, a) =>
            {
                if (!object.Equals(a.NewValue, a.OldValue) && s is LyricTextControl sender)
                {
                    sender.UpdateStrokeProperties();
                }
            }));



        public Brush KaraokeStroke
        {
            get { return (Brush)GetValue(KaraokeStrokeProperty); }
            set { SetValue(KaraokeStrokeProperty, value); }
        }

        // Using a DependencyProperty as the backing store for KaraokeStroke.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty KaraokeStrokeProperty =
            DependencyProperty.Register("KaraokeStroke", typeof(Brush), typeof(LyricTextControl), new PropertyMetadata(Brushes.Black, (s, a) =>
            {
                if (!object.Equals(a.NewValue, a.OldValue) && s is LyricTextControl sender)
                {
                    sender.UpdateStrokeProperties();
                }
            }));



        public bool IsStrokeEnabled
        {
            get { return (bool)GetValue(IsStrokeEnabledProperty); }
            set { SetValue(IsStrokeEnabledProperty, value); }
        }

        // Using a DependencyProperty as the backing store for IsStrokeEnabled.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IsStrokeEnabledProperty =
            DependencyProperty.Register("IsStrokeEnabled", typeof(bool), typeof(LyricTextControl), new PropertyMetadata(true, (s, a) =>
            {
                if (!object.Equals(a.NewValue, a.OldValue) && s is LyricTextControl sender)
                {
                    sender.UpdateStrokeProperties();
                }
            }));



        public ILrcFile? LrcFile
        {
            get { return (ILrcFile?)GetValue(LrcFileProperty); }
            set { SetValue(LrcFileProperty, value); }
        }

        // Using a DependencyProperty as the backing store for LrcFile.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty LrcFileProperty =
            DependencyProperty.Register("LrcFile", typeof(ILrcFile), typeof(LyricTextControl), new PropertyMetadata(null, (s, a) =>
            {
                if (!object.Equals(a.NewValue, a.OldValue) && s is LyricTextControl sender)
                {
                    sender.lrcFileWrapper = null;
                    if (a.NewValue is ILrcFile lrcFile)
                    {
                        sender.lrcFileWrapper = new LrcFileWrapper(lrcFile, sender.positionProvider.CurrentPosition, sender.SkipEmptyLine)
                        {
                            MediaDuration = sender.MediaDuration
                        };
                    }
                    sender.positionProvider.Reset((ILrcFile?)a.NewValue);
                    sender.positionProvider.ChangePosition(sender.Position + TimeSpan.FromSeconds(sender.TimeOffset));
                    sender.UpdateCurrentLyricLine();
                }
            }));



        public TimeSpan MediaDuration
        {
            get { return (TimeSpan)GetValue(MediaDurationProperty); }
            set { SetValue(MediaDurationProperty, value); }
        }

        public static readonly DependencyProperty MediaDurationProperty =
            DependencyProperty.Register("MediaDuration", typeof(TimeSpan), typeof(LyricTextControl), new PropertyMetadata(TimeSpan.Zero, (s, a) =>
            {
                if (!string.Equals(a.NewValue, a.OldValue) && s is LyricTextControl sender)
                {
                    if (sender.lrcFileWrapper != null)
                    {
                        sender.lrcFileWrapper.MediaDuration = (TimeSpan)a.NewValue;
                    }
                    sender.UpdateScrollOffset();
                    sender.UpdateKaraokeClip();
                }
            }));



        public TimeSpan Position
        {
            get { return (TimeSpan)GetValue(PositionProperty); }
            set { SetValue(PositionProperty, value); }
        }

        public static readonly DependencyProperty PositionProperty =
            DependencyProperty.Register("Position", typeof(TimeSpan), typeof(LyricTextControl), new PropertyMetadata(TimeSpan.Zero, (s, a) =>
            {
                if (!string.Equals(a.NewValue, a.OldValue) && s is LyricTextControl sender)
                {
                    sender.positionProvider.ChangePosition((TimeSpan)a.NewValue + TimeSpan.FromSeconds(sender.TimeOffset));
                }
            }));



        public double TimeOffset
        {
            get { return (double)GetValue(TimeOffsetProperty); }
            set { SetValue(TimeOffsetProperty, value); }
        }

        // Using a DependencyProperty as the backing store for TimeOffset.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty TimeOffsetProperty =
            DependencyProperty.Register("TimeOffset", typeof(double), typeof(LyricTextControl), new PropertyMetadata(0d, (s, a) =>
            {
                if (!string.Equals(a.NewValue, a.OldValue) && s is LyricTextControl sender)
                {
                    sender.positionProvider.ChangePosition(sender.Position + TimeSpan.FromSeconds((double)a.NewValue));
                }
            }));



        public double PlaybackRate
        {
            get { return (double)GetValue(PlaybackRateProperty); }
            set { SetValue(PlaybackRateProperty, value); }
        }

        // Using a DependencyProperty as the backing store for PlaybackRate.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty PlaybackRateProperty =
            DependencyProperty.Register("PlaybackRate", typeof(double), typeof(LyricTextControl), new PropertyMetadata(0d, (s, a) =>
            {
                if (!string.Equals(a.NewValue, a.OldValue) && s is LyricTextControl sender)
                {
                    sender.positionProvider.PlaybackRate = (double)a.NewValue;
                }
            }));



        public bool KaraokeEnabled
        {
            get { return (bool)GetValue(KaraokeEnabledProperty); }
            set { SetValue(KaraokeEnabledProperty, value); }
        }

        // Using a DependencyProperty as the backing store for karaokeEnabled.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty KaraokeEnabledProperty =
            DependencyProperty.Register("KaraokeEnabled", typeof(bool), typeof(LyricTextControl), new PropertyMetadata(false, (s, a) =>
             {
                 if (!string.Equals(a.NewValue, a.OldValue) && s is LyricTextControl sender)
                 {
                     sender.FirstRowAnimationPlaceholderText.Progress = (a.NewValue is true) ? 1 : 0;
                     sender.UpdateContainerWidth();
                 }
             }));



        public bool IsPlaying
        {
            get { return (bool)GetValue(IsPlayingProperty); }
            set { SetValue(IsPlayingProperty, value); }
        }

        // Using a DependencyProperty as the backing store for IsPlaying.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IsPlayingProperty =
            DependencyProperty.Register("IsPlaying", typeof(bool), typeof(LyricTextControl), new PropertyMetadata(false, (s, a) =>
            {
                if (!string.Equals(a.NewValue, a.OldValue) && s is LyricTextControl sender)
                {
                    sender.UpdateScrollOffset();
                    sender.UpdateKaraokeClip();
                }
            }));

        public string PlaceholderText1
        {
            get { return (string)GetValue(PlaceholderText1Property); }
            set { SetValue(PlaceholderText1Property, value); }
        }

        public static readonly DependencyProperty PlaceholderText1Property =
            DependencyProperty.Register("PlaceholderText1", typeof(string), typeof(LyricTextControl), new PropertyMetadata("", (s, a) =>
            {
                if (!string.Equals(a.NewValue, a.OldValue) && s is LyricTextControl sender)
                {
                    sender.UpdateCurrentLyricLine();
                }
            }));


        public string PlaceholderText2
        {
            get { return (string)GetValue(PlaceholderText2Property); }
            set { SetValue(PlaceholderText2Property, value); }
        }

        public static readonly DependencyProperty PlaceholderText2Property =
            DependencyProperty.Register("PlaceholderText2", typeof(string), typeof(LyricTextControl), new PropertyMetadata("", (s, a) =>
            {
                if (!string.Equals(a.NewValue, a.OldValue) && s is LyricTextControl sender)
                {
                    sender.UpdateCurrentLyricLine();
                }
            }));

        public bool SkipEmptyLine
        {
            get { return (bool)GetValue(SkipEmptyLineProperty); }
            set { SetValue(SkipEmptyLineProperty, value); }
        }

        // Using a DependencyProperty as the backing store for SkipEmptyLine.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SkipEmptyLineProperty =
            DependencyProperty.Register("SkipEmptyLine", typeof(bool), typeof(LyricTextControl), new PropertyMetadata(false, (s, a) =>
            {
                if (!string.Equals(a.NewValue, a.OldValue) && s is LyricTextControl sender)
                {
                    if (sender.lrcFileWrapper != null)
                    {
                        sender.lrcFileWrapper.SkipEmptyLine = a.NewValue is true;
                    }
                    sender.UpdateCurrentLyricLine();
                }
            }));



        public bool IsOpacityMaskEnabled
        {
            get { return (bool)GetValue(IsOpacityMaskEnabledProperty); }
            set { SetValue(IsOpacityMaskEnabledProperty, value); }
        }

        // Using a DependencyProperty as the backing store for IsOpacityMaskEnabled.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IsOpacityMaskEnabledProperty =
            DependencyProperty.Register("IsOpacityMaskEnabled", typeof(bool), typeof(LyricTextControl), new PropertyMetadata(true, (s, a) =>
            {
                if (!string.Equals(a.NewValue, a.OldValue) && s is LyricTextControl sender)
                {
                    sender.UpdateOpacityMask();
                }
            }));



        public bool IsEmpty
        {
            get { return (bool)GetValue(IsEmptyProperty); }
            private set { SetValue(IsEmptyPropertyKey, value); }
        }

        private static readonly DependencyPropertyKey IsEmptyPropertyKey;
        public static readonly DependencyProperty IsEmptyProperty;



        public bool LowFrameRateMode
        {
            get { return (bool)GetValue(LowFrameRateModeProperty); }
            set { SetValue(LowFrameRateModeProperty, value); }
        }

        // Using a DependencyProperty as the backing store for LowFrameRateMode.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty LowFrameRateModeProperty =
            DependencyProperty.Register("LowFrameRateMode", typeof(bool), typeof(LyricTextControl), new PropertyMetadata(false, (s, a) =>
            {
                if (!string.Equals(a.NewValue, a.OldValue) && s is LyricTextControl sender)
                {
                    sender.UpdateAnimationPlaceholder();
                    sender.UpdateContainerWidth();
                }
            }));



        public LyricTextControl TranslationLyricControl
        {
            get { return (LyricTextControl)GetValue(TranslationLyricControlProperty); }
            set { SetValue(TranslationLyricControlProperty, value); }
        }

        // Using a DependencyProperty as the backing store for TranslationLyricControl.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty TranslationLyricControlProperty =
            DependencyProperty.Register("TranslationLyricControl", typeof(LyricTextControl), typeof(LyricTextControl), new PropertyMetadata(null, (s, a) =>
            {
                if (!string.Equals(a.NewValue, a.OldValue) && s is LyricTextControl sender)
                {
                    sender.TranslationContainer.Child = a.NewValue as UIElement;
                    sender.TranslationContainer.UpdateLayout();

                    if (a.OldValue is LyricTextControl oldValue)
                    {
                        oldValue.isTranslationControl = false;
                        oldValue.LyricLineChanged -= sender.TranslationLyricControl_LyricLineChanged;
                        oldValue.UpdateAnimationPlaceholder();
                    }
                    if (a.NewValue is LyricTextControl newValue)
                    {
                        newValue.isTranslationControl = true;
                        newValue.LyricLineChanged += sender.TranslationLyricControl_LyricLineChanged;
                        newValue.UpdateAnimationPlaceholder();
                    }

                    sender.UpdateSecondLine();
                }
            }));

        public SecondRowType SecondRowType
        {
            get { return (SecondRowType)GetValue(SecondRowTypeProperty); }
            set { SetValue(SecondRowTypeProperty, value); }
        }

        // Using a DependencyProperty as the backing store for SecondRowType.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SecondRowTypeProperty =
            DependencyProperty.Register("SecondRowType", typeof(SecondRowType), typeof(LyricTextControl), new PropertyMetadata(SecondRowType.TranslationOrNextLyric, (s, a) =>
            {
                if (!string.Equals(a.NewValue, a.OldValue) && s is LyricTextControl sender)
                {
                    sender.UpdateCurrentLyricLine();
                }
            }));



        public bool IsSecondRowVisible
        {
            get { return (bool)GetValue(IsSecondRowVisibleProperty); }
            private set { SetValue(IsSecondRowVisiblePropertyKey, value); }
        }

        private static readonly DependencyPropertyKey IsSecondRowVisiblePropertyKey;
        public static readonly DependencyProperty IsSecondRowVisibleProperty;



        public HorizontalAlignment LyricHorizontalAlignment
        {
            get { return (HorizontalAlignment)GetValue(LyricHorizontalAlignmentProperty); }
            set { SetValue(LyricHorizontalAlignmentProperty, value); }
        }

        // Using a DependencyProperty as the backing store for LyricHorizontalAlignment.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty LyricHorizontalAlignmentProperty =
            DependencyProperty.Register("LyricHorizontalAlignment", typeof(HorizontalAlignment), typeof(LyricTextControl), new PropertyMetadata(HorizontalAlignment.Left, (s, a) =>
            {
                if (s is LyricTextControl sender)
                {
                    sender.UpdateScrollOffset();
                    sender.UpdateKaraokeClip();
                    sender.StartRowAnimation(TimeSpan.Zero);
                }
            }));

        private void OnLyricLineChanged()
        {
            if (IsLoaded)
            {
                LyricLineChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        private void OnIsSecondRowVisibleChanged()
        {
            IsSecondRowVisibleChanged?.Invoke(this, EventArgs.Empty);
        }

        public event EventHandler? LyricLineChanged;

        public event EventHandler? IsSecondRowVisibleChanged;

        private void LyricTextControl_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            TextContainer.UpdateLayout();
            UpdateContainerWidth();
        }

        private void LyricTextControl_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            TextContainer.UpdateLayout();
            UpdateContainerWidth();
        }


        private void PositionProvider_PositionChanged(object? sender, EventArgs e)
        {
            UpdateCurrentLyricLine();
        }


        private void TranslationLyricControl_LyricLineChanged(object? sender, EventArgs e)
        {
            UpdateSecondLine();
        }


        private void UpdateAnimationPlaceholder()
        {
            if (LowFrameRateMode || isTranslationControl || !IsSecondRowVisible)
            {
                FirstRowAnimationPlaceholderBorder.Visibility = Visibility.Collapsed;
            }
            else
            {
                FirstRowAnimationPlaceholderBorder.Visibility = Visibility.Visible;
            }
        }

        private void UpdateSecondLine()
        {
            if (isTranslationControl)
            {
                SecondRowBorder.Visibility = Visibility.Collapsed;
                TranslationContainer.Visibility = Visibility.Collapsed;
                SecondRowHeight.Height = new GridLength(0);
                return;
            }

            //bool hide = false;

            TranslationLyricControl?.UpdateCurrentLyricLine();
            bool translationVisible = TranslationLyricControl?.IsEmpty == false;
            bool nextLineVisible = !string.IsNullOrEmpty(NextLineContentTextBlock.Text);

            if (SecondRowType == SecondRowType.Collapsed)
            {
                translationVisible = false;
                nextLineVisible = false;
            }
            else if (SecondRowType == SecondRowType.TranslationOnly)
            {
                // 仅显示翻译时，如果没有翻译则隐藏第二行
                nextLineVisible = false;
            }
            else if (SecondRowType == SecondRowType.NextLyricOnly)
            {
                // 仅显示下一行歌词时，即使下一行为空也显示
                translationVisible = false;
                nextLineVisible = true;
            }
            else if (SecondRowType == SecondRowType.TranslationOrNextLyric)
            {
                // 如果无翻译则显示下一行歌词，即使下一行为空也显示
                nextLineVisible = !translationVisible;
            }

            if (translationVisible || nextLineVisible)
            {
                TranslationContainer.Visibility = translationVisible ? Visibility.Visible : Visibility.Collapsed;
                SecondRowBorder.Visibility = nextLineVisible ? Visibility.Visible : Visibility.Collapsed;

                SecondRowHeight.Height = new GridLength(1, GridUnitType.Star);
            }
            else
            {
                SecondRowBorder.Visibility = Visibility.Collapsed;
                TranslationContainer.Visibility = Visibility.Collapsed;
                SecondRowHeight.Height = new GridLength(0);
            }

            TranslationContainer.UpdateLayout();
            SecondRowBorder.UpdateLayout();

            IsSecondRowVisible = translationVisible || nextLineVisible;

            if (IsSecondRowVisible)
            {
                if (nextLineVisible)
                {
                    UpdateNextLineTextOffset();
                }
                else
                {
                    StartRowAnimation(TimeSpan.Zero);
                }
            }
        }

        private void UpdateOpacityMask()
        {
            if (IsOpacityMaskEnabled)
            {
                FirstRowAnimationPlaceholderBorder.OpacityMask = maskBrush;
                FirstRowBorder.OpacityMask = maskBrush;
                SecondRowBorder.OpacityMask = maskBrush;
                TranslationContainer.OpacityMask = maskBrush;

                maskBrush.EndPoint = new Point(this.ActualWidth, 0);

                if (TextContainer.ActualHeight > 0)
                {
                    var scale = ContentTextBlock.FontSize / 20;
                    var margin = 12d;
                    if (scale < 1)
                    {
                        margin *= scale;
                    }

                    maskBrush.GradientStops[1].Offset = margin / this.ActualWidth;
                    maskBrush.GradientStops[2].Offset = 1 - margin / this.ActualWidth;
                }
            }
            else
            {
                FirstRowAnimationPlaceholderBorder.OpacityMask = null;
                FirstRowBorder.OpacityMask = null;
                SecondRowBorder.OpacityMask = null;
                TranslationContainer.OpacityMask = null;
            }
        }

        private void UpdateCurrentLyricLine()
        {
            bool flag = false;
            bool scrollNext = false;

            if (lrcFileWrapper != null)
            {
                var lrcLine = lrcFileWrapper.CurrentLine;
                var oldNextLine = lrcFileWrapper.NextLine;

                lrcFileWrapper.Position = positionProvider.CurrentPosition;

                IOneLineLyric? newLrcLine = lrcFileWrapper.CurrentLine;

                scrollNext = oldNextLine != null && oldNextLine == newLrcLine;

                flag = newLrcLine != lrcLine;

                FirstRowAnimationPlaceholderText.Text = lrcLine?.Content?.Trim() ?? "";
                ContentTextBlock.Text = newLrcLine?.Content?.Trim() ?? "";
                NextLineContentTextBlock.Text = lrcFileWrapper?.NextLine?.Content?.Trim() ?? "";

                IsEmpty = string.IsNullOrWhiteSpace(newLrcLine?.Content);
            }
            else
            {
                IsEmpty = true;

                if (SecondRowType == SecondRowType.TranslationOrNextLyric)
                {
                    ContentTextBlock.Text = PlaceholderText1?.Trim() ?? "";
                    NextLineContentTextBlock.Text = PlaceholderText2?.Trim() ?? "";
                }
                else
                {
                    var str = PlaceholderText1?.Trim();
                    if (!string.IsNullOrEmpty(PlaceholderText1) && !string.IsNullOrEmpty(PlaceholderText2))
                    {
                        str = $"{PlaceholderText1.Trim()} - {PlaceholderText2.Trim()}";
                    }
                    ContentTextBlock.Text = str ?? "";
                }
                FirstRowAnimationPlaceholderText.Text = "";
            }
            TextContainer.UpdateLayout();

            UpdateSecondLine();
            UpdateContainerWidth();

            if (scrollNext)
            {
                StartRowAnimation(lrcFileWrapper?.CurrentLineDuration ?? TimeSpan.Zero);
            }

            if (flag)
            {
                OnLyricLineChanged();
            }
        }

        private void UpdateContainerWidth()
        {
            curRowSb?.SkipToFill();
            curRowSb = null;

            var textHeight = ContentBorder.ActualHeight;
            if (ActualHeight == 0 || textHeight == 0) return;

            UpdateOpacityMask();

            UpdateScrollOffset();
            UpdateKaraokeClip();
        }

        private TimeSpan GetActualDuration(bool offset)
        {
            var duration = TimeSpan.Zero;

            if (lrcFileWrapper?.CurrentLine != null && !lrcFileWrapper.ActualNextLine)
            {
                duration = lrcFileWrapper.CurrentLineDuration;

                if (duration.TotalSeconds <= 0)
                {
                    duration = TimeSpan.Zero;
                }
                else if (offset && duration.TotalSeconds > 1.6)
                {
                    duration = TimeSpan.FromSeconds(duration.TotalSeconds - 0.8);
                }
            }

            return duration;
        }

        private void UpdateNextLineTextOffset()
        {
            if (isTranslationControl || !NextLineContentBorder.IsVisible) return;

            var scrollableWidth = NextLineContentBorder.ActualWidth - TextContainer.ActualWidth;

            var offset = LyricHorizontalAlignment switch
            {
                HorizontalAlignment.Left => 0,
                HorizontalAlignment.Right => Math.Max(0, -scrollableWidth),
                _ => Math.Max(0, -scrollableWidth / 2),
            };
            NextLineTrans.X = offset;
        }

        private void UpdateScrollOffset()
        {
            curSb?.SkipToFill();
            curSb = null;

            bool actualNextLine = lrcFileWrapper?.ActualNextLine == true;
            var duration = GetActualDuration(true);
            var startTime = lrcFileWrapper?.CurrentLine?.Timestamp ?? TimeSpan.Zero;
            var position = positionProvider.CurrentPosition;
            var scrollableWidth = ContentBorder.ActualWidth - TextContainer.ActualWidth;

            var from = 0d;
            var to = 0d;

            bool disabledAnimation = false;

            if (actualNextLine || duration.TotalMilliseconds < 0.01 || scrollableWidth < 0)
            {
                if (actualNextLine && scrollableWidth >= 0)
                {
                    to = 0;
                }
                else
                {
                    to = LyricHorizontalAlignment switch
                    {
                        HorizontalAlignment.Left => 0,
                        HorizontalAlignment.Right => -scrollableWidth,
                        _ => -scrollableWidth / 2,
                    };
                }
                disabledAnimation = true;
            }
            else
            {
                to = -scrollableWidth;

                if (position > startTime && position < startTime + duration)
                {
                    from = (position.Ticks - startTime.Ticks) * 1.0 / duration.Ticks * (-scrollableWidth);
                    if (!IsPlaying)
                    {
                        disabledAnimation = true;
                        to = from;
                    }
                }
                else if (position >= startTime + duration)
                {
                    disabledAnimation = true;
                    from = -scrollableWidth;
                }
            }


            AnimationToOffset(from, to, disabledAnimation ? TimeSpan.Zero : startTime + duration - position);

            UpdateNextLineTextOffset();
        }

        private void UpdateKaraokeClip()
        {
            curClipSb?.SkipToFill();
            curClipSb = null;

            if (!KaraokeEnabled || lrcFileWrapper?.CurrentLine == null)
            {
                AnimationClip(0, 0, TimeSpan.Zero);
                return;
            }

            var duration = GetActualDuration(false);
            var startTime = lrcFileWrapper?.CurrentLine?.Timestamp ?? TimeSpan.Zero;
            var position = positionProvider.CurrentPosition;

            if (duration.TotalMilliseconds <= 0.01)
            {
                AnimationClip(0, 0, TimeSpan.Zero);
                return;
            }

            var progress = (position.TotalMilliseconds - startTime.TotalMilliseconds) / duration.TotalMilliseconds;

            if (progress <= -0.01)
            {
                AnimationClip(0, 0, TimeSpan.Zero);
            }
            else if (progress > 1)
            {
                AnimationClip(1, 1, TimeSpan.Zero);
            }
            else
            {
                if (IsPlaying)
                {
                    AnimationClip(progress, 1, startTime + duration - position);
                }
                else
                {
                    AnimationClip(progress, progress, TimeSpan.Zero);
                }
            }
        }

        private void AnimationToOffset(double? from, double to, TimeSpan duration)
        {
            curSb?.SkipToFill();
            curSb = null;
            var playbackRate = positionProvider.PlaybackRate;

            var sb = new Storyboard();
            var an = new DoubleAnimation()
            {
                From = from,
                To = to,
                Duration = TimeSpan.FromSeconds(Math.Max(0, duration.TotalSeconds))
            };

            sb.Children.Add(an);

            Storyboard.SetTarget(an, ContentBorder);
            Storyboard.SetTargetProperty(an, new PropertyPath("(UIElement.RenderTransform).(TransformGroup.Children)[1].(TranslateTransform.X)"));

            sb.SpeedRatio = playbackRate > 0 ? playbackRate : 1;

            if (LowFrameRateMode)
            {
                Timeline.SetDesiredFrameRate(sb, 15);
            }

            sb.Freeze();

            sb.Begin();
            curSb = sb;
        }

        private void AnimationClip(double from, double to, TimeSpan duration)
        {
            curClipSb?.SkipToFill();
            curClipSb = null;

            var playbackRate = positionProvider.PlaybackRate;

            var sb = new Storyboard();
            var an = new DoubleAnimation()
            {
                From = from,
                To = to,
                Duration = TimeSpan.FromSeconds(Math.Max(0, duration.TotalSeconds)),
            };
            sb.Children.Add(an);

            Storyboard.SetTarget(an, ContentTextBlock);
            Storyboard.SetTargetProperty(an, new PropertyPath("Progress"));

            sb.SpeedRatio = playbackRate > 0 ? playbackRate : 1;

            if (LowFrameRateMode)
            {
                Timeline.SetDesiredFrameRate(sb, 15);
            }

            sb.Freeze();

            sb.Begin();

            curClipSb = sb;
        }


        private void StartRowAnimation(TimeSpan duration)
        {
            if (isTranslationControl) return;

            curRowSb?.SkipToFill();
            curRowSb = null;

            const double durationSeconds = 0.45;
            const double minDurationSeconds = 0.1;

            if (IsSecondRowVisible
                && !string.IsNullOrEmpty(FirstRowAnimationPlaceholderText.Text)
                && TranslationContainer.Visibility == Visibility.Collapsed
                && !LowFrameRateMode
                && duration.TotalSeconds > minDurationSeconds / (positionProvider?.PlaybackRate ?? 1))
            {
                var rowAnDuration = TimeSpan.FromSeconds(Math.Min(duration.TotalSeconds, durationSeconds));

                var easingFunc = new CubicEase()
                {
                    EasingMode = EasingMode.EaseOut,
                };

                // 更新第一行占位符的位置
                UpdateFirstRowPlaceholderTransformProperties();

                // 更新第一行位置
                UpdateFirstRowTransformProperties();

                var sb = new Storyboard();

                #region First Row Placeholder Animations

                var firstRowPlaceholderOpacityAn = new DoubleAnimationUsingKeyFrames()
                {
                    Duration = new Duration(rowAnDuration / 2),
                    BeginTime = TimeSpan.Zero,
                    KeyFrames =
                    {
                        new DiscreteDoubleKeyFrame(1, KeyTime.FromTimeSpan(TimeSpan.Zero)),
                        //new DiscreteDoubleKeyFrame(1, KeyTime.FromTimeSpan(rowAnDuration / 3)),
                        new LinearDoubleKeyFrame(0, KeyTime.FromTimeSpan(rowAnDuration / 2)),
                    }
                };
                Storyboard.SetTarget(firstRowPlaceholderOpacityAn, FirstRowAnimationPlaceholderBorder);
                Storyboard.SetTargetProperty(firstRowPlaceholderOpacityAn, new PropertyPath("Opacity"));
                sb.Children.Add(firstRowPlaceholderOpacityAn);

                var firstRowPlaceholderTransAn = CreateDoubleAnimation(
                    _from: 0,
                    _to: -SecondRowBorder.ActualHeight,
                    _duration: rowAnDuration,
                    _beginTime: TimeSpan.Zero,
                    _target: FirstRowAnimationPlaceholderBorder,
                    _propertyPath: "(UIElement.RenderTransform).(TranslateTransform.Y)");
                sb.Children.Add(firstRowPlaceholderTransAn);

                var firstRowPlaceholderScaleXAn = CreateDoubleAnimation(
                    _from: 1,
                    _to: SecondRowBorder.ActualHeight / FirstRowBorder.ActualHeight,
                    _duration: rowAnDuration,
                    _beginTime: TimeSpan.Zero,
                    _target: FirstRowAnimationPlaceholderContainerBorder,
                    _propertyPath: "(UIElement.RenderTransform).(TransformGroup.Children)[0].(ScaleTransform.ScaleX)");
                sb.Children.Add(firstRowPlaceholderScaleXAn);

                var firstRowPlaceholderScaleYAn = CreateDoubleAnimation(
                    _from: 1,
                    _to: SecondRowBorder.ActualHeight / FirstRowBorder.ActualHeight,
                    _duration: rowAnDuration,
                    _beginTime: TimeSpan.Zero,
                    _target: FirstRowAnimationPlaceholderContainerBorder,
                    _propertyPath: "(UIElement.RenderTransform).(TransformGroup.Children)[0].(ScaleTransform.ScaleY)");
                sb.Children.Add(firstRowPlaceholderScaleYAn);

                #endregion First Row Placeholder Animations

                #region First Row Animations

                var firstRowTransYAn = CreateDoubleAnimation(
                    _from: FirstRowBorder.ActualHeight,
                    _to: 0,
                    _duration: rowAnDuration,
                    _beginTime: TimeSpan.Zero,
                    _target: FirstRowBorder,
                    _propertyPath: "(UIElement.RenderTransform).(TranslateTransform.Y)");
                sb.Children.Add(firstRowTransYAn);

                var firstRowScaleXAn = CreateDoubleAnimation(
                    _from: SecondRowBorder.ActualHeight / FirstRowBorder.ActualHeight,
                    _to: 1,
                    _duration: rowAnDuration,
                    _beginTime: TimeSpan.Zero,
                    _target: ContentBorder,
                    _propertyPath: "(UIElement.RenderTransform).(TransformGroup.Children)[0].(ScaleTransform.ScaleX)");
                sb.Children.Add(firstRowScaleXAn);

                var firstRowScaleYAn = CreateDoubleAnimation(
                    _from: SecondRowBorder.ActualHeight / FirstRowBorder.ActualHeight,
                    _to: 1,
                    _duration: rowAnDuration,
                    _beginTime: TimeSpan.Zero,
                    _target: ContentBorder,
                    _propertyPath: "(UIElement.RenderTransform).(TransformGroup.Children)[0].(ScaleTransform.ScaleY)");
                sb.Children.Add(firstRowScaleYAn);

                #endregion First Row Animations

                #region Second Row Animations

                var secondRowTransAn = CreateDoubleAnimation(
                    _from: FirstRowBorder.ActualHeight,
                    _to: 0,
                    _duration: rowAnDuration,
                    _beginTime: TimeSpan.Zero,
                    _target: SecondRowBorder,
                    _propertyPath: "(UIElement.RenderTransform).(TranslateTransform.Y)");
                sb.Children.Add(secondRowTransAn);

                #endregion Second Row Animations

                sb.Freeze();

                sb.Begin();
                curRowSb = sb;

                DoubleAnimation CreateDoubleAnimation(double _from, double _to, TimeSpan _duration, TimeSpan _beginTime, DependencyObject _target, string _propertyPath)
                {
                    var _an = new DoubleAnimation()
                    {
                        From = _from,
                        To = _to,
                        Duration = new Duration(_duration),
                        BeginTime = _beginTime,
                        EasingFunction = easingFunc
                    };
                    Storyboard.SetTarget(_an, _target);
                    Storyboard.SetTargetProperty(_an, new PropertyPath(_propertyPath));

                    return _an;
                }
            }
        }


        private void UpdateFirstRowPlaceholderTransformProperties()
        {

            if (FirstRowAnimationPlaceholderContainerBorder.ActualWidth > 0)
            {
                double offsetX = 0;
                if (LyricHorizontalAlignment == HorizontalAlignment.Right)
                {
                    // 右对齐，无需特殊处理
                    offsetX = FirstRowAnimationPlaceholderBorder.ActualWidth - FirstRowAnimationPlaceholderContainerBorder.ActualWidth;
                    FirstRowAnimationPlaceholderContainerBorder.RenderTransformOrigin = new Point((FirstRowAnimationPlaceholderContainerBorder.ActualWidth - FirstRowAnimationPlaceholderContainerBorder.Padding.Right) / FirstRowAnimationPlaceholderContainerBorder.ActualWidth, 0);
                }
                else if (FirstRowAnimationPlaceholderContainerBorder.ActualWidth > FirstRowAnimationPlaceholderBorder.ActualWidth)
                {
                    // 文本超出视口

                    var scale = SecondRowBorder.ActualHeight / FirstRowAnimationPlaceholderBorder.ActualHeight;
                    var scaledWidth = FirstRowAnimationPlaceholderContainerBorder.ActualWidth * scale;
                    var viewport = FirstRowAnimationPlaceholderBorder.ActualWidth;

                    if (scaledWidth > viewport)
                    {
                        // 缩放后依旧超出视口，同右对齐处理
                        FirstRowAnimationPlaceholderContainerBorder.RenderTransformOrigin = new Point((FirstRowAnimationPlaceholderContainerBorder.ActualWidth - FirstRowAnimationPlaceholderContainerBorder.Padding.Right) / FirstRowAnimationPlaceholderContainerBorder.ActualWidth, 0);
                    }
                    else if (LyricHorizontalAlignment == HorizontalAlignment.Left)
                    {
                        var centerPointX = (scaledWidth - viewport * scale) / (1 - scale);

                        FirstRowAnimationPlaceholderContainerBorder.RenderTransformOrigin = new Point(centerPointX / FirstRowAnimationPlaceholderContainerBorder.ActualWidth, 0);
                    }
                    else
                    {
                        var centerPointX = ((viewport + scaledWidth) / 2 - viewport * scale) / (1 - scale);
                        FirstRowAnimationPlaceholderContainerBorder.RenderTransformOrigin = new Point(centerPointX / FirstRowAnimationPlaceholderContainerBorder.ActualWidth, 0);
                    }

                    offsetX = FirstRowAnimationPlaceholderBorder.ActualWidth - FirstRowAnimationPlaceholderContainerBorder.ActualWidth;
                }
                else if (LyricHorizontalAlignment == HorizontalAlignment.Left)
                {
                    offsetX = 0;
                    FirstRowAnimationPlaceholderContainerBorder.RenderTransformOrigin = new Point(FirstRowAnimationPlaceholderContainerBorder.Padding.Left / FirstRowAnimationPlaceholderContainerBorder.ActualWidth, 0);
                }
                else
                {
                    offsetX = (FirstRowAnimationPlaceholderBorder.ActualWidth - FirstRowAnimationPlaceholderContainerBorder.ActualWidth) / 2;
                    FirstRowAnimationPlaceholderContainerBorder.RenderTransformOrigin = new Point(0.5, 0);
                }

                ((TranslateTransform)FirstRowAnimationPlaceholderTrans.Children[1]).X = offsetX;
            }

        }

        private void UpdateFirstRowTransformProperties()
        {
            if (ContentBorder.ActualWidth > 0)
            {
                if (LyricHorizontalAlignment == HorizontalAlignment.Left)
                {
                    // 左对齐，无需特殊处理
                    ContentBorder.RenderTransformOrigin = new Point(ContentBorder.Padding.Left / ContentBorder.ActualWidth, 0);
                }
                else if (ContentBorder.ActualWidth > FirstRowBorder.ActualWidth)
                {
                    // 文本超出视口

                    var scale = SecondRowBorder.ActualHeight / FirstRowBorder.ActualHeight;
                    var scaledWidth = ContentBorder.ActualWidth * scale;
                    var viewport = FirstRowBorder.ActualWidth;

                    if (scaledWidth > viewport)
                    {
                        // 缩放后依旧超出视口，同左对齐处理
                        ContentBorder.RenderTransformOrigin = new Point(ContentBorder.Padding.Left / ContentBorder.ActualWidth, 0);
                    }
                    else if (LyricHorizontalAlignment == HorizontalAlignment.Right)
                    {
                        var centerPointX = (scaledWidth - viewport) / (scale - 1);
                        ContentBorder.RenderTransformOrigin = new Point(centerPointX / ContentBorder.ActualWidth, 0);
                    }
                    else
                    {
                        var centerPointX = (scaledWidth - viewport - ContentBorder.Padding.Left) / 2 / (scale - 1);
                        ContentBorder.RenderTransformOrigin = new Point(centerPointX / ContentBorder.ActualWidth, 0);
                    }
                }
                else if (LyricHorizontalAlignment == HorizontalAlignment.Right)
                {
                    ContentBorder.RenderTransformOrigin = new Point((ContentBorder.ActualWidth - ContentBorder.Padding.Right) / ContentBorder.ActualWidth, 0);
                }
                else
                {
                    ContentBorder.RenderTransformOrigin = new Point(0.5, 0);
                }

            }

        }

        private void ContentBorder_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            //TextBlockHost.Width = TextContainer.ActualWidth;
            //TextBlockHost.Height = e.NewSize.Height;
        }

        private void UpdateStrokeProperties()
        {
            var enabled = IsStrokeEnabled;
            var k_enabled = IsStrokeEnabled;

            if (enabled && Stroke == null) enabled = false;
            if (k_enabled && KaraokeStroke == null) k_enabled = false;

            if (enabled && Stroke is SolidColorBrush brush && (brush.Opacity <= 0.01 || brush.Color.A < 1)) enabled = false;
            if (k_enabled && KaraokeStroke is SolidColorBrush kbrush && (kbrush.Opacity <= 0.01 || kbrush.Color.A < 1)) k_enabled = false;

            if (enabled)
            {
                ContentTextBlock.StrokeThickness1 = double.NaN;
                ContentTextBlock.Stroke1 = Stroke;
                NextLineContentTextBlock.StrokeThickness1 = double.NaN;
                NextLineContentTextBlock.Stroke1 = Stroke;
            }
            else
            {
                ContentTextBlock.StrokeThickness1 = 0;
                ContentTextBlock.Stroke1 = null;
                NextLineContentTextBlock.StrokeThickness1 = 0;
                NextLineContentTextBlock.Stroke1 = null;
            }

            if (k_enabled)
            {
                ContentTextBlock.StrokeThickness2 = double.NaN;
                ContentTextBlock.Stroke2 = KaraokeStroke;
            }
            else
            {
                ContentTextBlock.StrokeThickness2 = 0;
                ContentTextBlock.Stroke2 = null;
            }
        }
    }

    public enum SecondRowType
    {
        Collapsed,
        TranslationOnly,
        TranslationOrNextLyric,
        NextLyricOnly
    }
}
