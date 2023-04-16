using HotLyric.Win32.Controls.LyricControlDrawingData;
using HotLyric.Win32.Utils;
using HotLyric.Win32.Utils.LrcProviders;
using HotLyric.Win32.Utils.LyricFiles;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Brushes;
using Microsoft.Graphics.Canvas.Effects;
using Microsoft.Graphics.Canvas.UI.Xaml;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Media.Ocr;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using Windows.UI;
using HotLyric.Win32.Models;
using System.Globalization;

//https://go.microsoft.com/fwlink/?LinkId=234236 上介绍了“用户控件”项模板

namespace HotLyric.Win32.Controls
{
    public sealed partial class LyricControl : UserControl
    {
        private DependencyPropertiesObserver propObserver;
        private LyricDrawingLine? drawingMainLine;
        private LyricDrawingLine? drawingRemovingLine;
        private LyricDrawingLine? drawingSecondaryLine;
        private LyricDrawingTextColors themeColors;
        private LyricDrawingTextColors? colors;
        private CircleEase easingFunc;

        private LyricLines lyricLines;

        public LyricControl()
        {
            this.InitializeComponent();

            lyricLines = new LyricLines();

            themeColors = CreateDefaultColors();

            propObserver = new DependencyPropertiesObserver(this);
            propObserver[PaddingProperty]?.AddHandler((s, a) => Refresh());
            propObserver[OpacityProperty]?.AddHandler((s, a) => Refresh());
            propObserver[ClipToPaddingProperty]?.AddHandler((s, a) => Refresh());
            propObserver[LineModeProperty]?.AddHandler((s, a) => Refresh());
            propObserver[LineSpaceProperty]?.AddHandler((s, a) => Refresh());
            propObserver[LineAlignmentProperty]?.AddHandler((s, a) => Refresh());
            propObserver[LyricProperty]?.AddHandler((s, a) => Refresh());
            propObserver[PausedProperty]?.AddHandler((s, a) => Refresh());
            propObserver[TextStrokeTypeProperty]?.AddHandler((s, a) => Refresh());
            propObserver[ProgressAnimationModeProperty]?.AddHandler((s, a) => Refresh());
            propObserver[ScrollAnimationModeProperty]?.AddHandler((s, a) => Refresh());
            propObserver[MediaDurationProperty]?.AddHandler((s, a) => Refresh());
            propObserver[TextOpacityMaskProperty]?.AddHandler((s, a) => Refresh());
            propObserver[ThemeProperty]?.AddHandler((s, a) =>
            {
                if (a.NewValue is LyricThemeView theme)
                {
                    GetColor(c => themeColors.FillColor1 = c, theme.LyricBrush);
                    GetColor(c => themeColors.StrokeColor1 = c, theme.LyricStrokeBrush);
                    GetColor(c => themeColors.FillColor2 = c, theme.KaraokeBrush);
                    GetColor(c => themeColors.StrokeColor2 = c, theme.KaraokeStrokeBrush);
                }
                else
                {
                    themeColors = CreateDefaultColors();
                }

                Refresh();

                void GetColor(Action<Windows.UI.Color> _action, Microsoft.UI.Xaml.Media.Brush? _brush)
                {
                    if (_brush is Microsoft.UI.Xaml.Media.SolidColorBrush _sb)
                    {
                        _action.Invoke(_sb.Color.WithOpacity(_sb.Opacity));
                    }
                }
            });
            propObserver[LyricFontFamilyProperty]?.AddHandler((s, a) => Refresh());
            propObserver[IsLyricTranslateEnabledProperty]?.AddHandler((s, a) =>
            {
                if (lyricLines != null)
                {
                    lyricLines.IsTranslateEnabled = a.NewValue is true;
                    Refresh();
                }
            });

            propObserver[LowFrameRateModeProperty]?.AddHandler((s, a) =>
            {
                if (a.NewValue is true)
                {
                    CanvasControl.TargetElapsedTime = TimeSpan.FromSeconds(1 / 20d);
                }
                else
                {
                    CanvasControl.TargetElapsedTime = TimeSpan.FromSeconds(1 / 60d);
                }

                Refresh();
            });


            easingFunc = new CircleEase()
            {
                EasingMode = EasingMode.EaseOut,
            };

            CanvasControl.IsFixedTimeStep = true;

            if (LowFrameRateMode)
            {
                CanvasControl.TargetElapsedTime = TimeSpan.FromSeconds(1 / 20d);
            }
            else
            {
                CanvasControl.TargetElapsedTime = TimeSpan.FromSeconds(1 / 60d);
            }

            this.Loaded += LyricControl_Loaded;
            this.Unloaded += LyricControl_Unloaded;
            this.SizeChanged += LyricControl_SizeChanged;
        }

        public LyricControlLineMode LineMode
        {
            get { return (LyricControlLineMode)GetValue(LineModeProperty); }
            set { SetValue(LineModeProperty, value); }
        }

        public static readonly DependencyProperty LineModeProperty =
            DependencyProperty.Register("LineMode", typeof(LyricControlLineMode), typeof(LyricControl), new PropertyMetadata(LyricControlLineMode.DoubleLine));



        public double LineSpace
        {
            get { return (double)GetValue(LineSpaceProperty); }
            set { SetValue(LineSpaceProperty, value); }
        }

        public static readonly DependencyProperty LineSpaceProperty =
            DependencyProperty.Register("LineSpace", typeof(double), typeof(LyricControl), new PropertyMetadata(0d));



        public bool ClipToPadding
        {
            get { return (bool)GetValue(ClipToPaddingProperty); }
            set { SetValue(ClipToPaddingProperty, value); }
        }

        public static readonly DependencyProperty ClipToPaddingProperty =
            DependencyProperty.Register("ClipToPadding", typeof(bool), typeof(LyricControl), new PropertyMetadata(false));

        public LyricDrawingLineAlignment LineAlignment
        {
            get { return (LyricDrawingLineAlignment)GetValue(LineAlignmentProperty); }
            set { SetValue(LineAlignmentProperty, value); }
        }

        public static readonly DependencyProperty LineAlignmentProperty =
            DependencyProperty.Register("LineAlignment", typeof(LyricDrawingLineAlignment), typeof(LyricControl), new PropertyMetadata(LyricDrawingLineAlignment.Left));



        public Lyric Lyric
        {
            get { return (Lyric)GetValue(LyricProperty); }
            set { SetValue(LyricProperty, value); }
        }

        public static readonly DependencyProperty LyricProperty =
            DependencyProperty.Register("Lyric", typeof(Lyric), typeof(LyricControl), new PropertyMetadata(null, (s, a) =>
            {
                if (s is LyricControl sender && !Equals(a.NewValue, a.OldValue))
                {
                    sender.lyricLines.Lyric = (Lyric?)a.NewValue;
                }
            }));

        private TimeSpan lastPosition;
        private TimeSpan totalTimeOffset;
        private TimeSpan totalTime;

        public TimeSpan Position
        {
            get { return (TimeSpan)GetValue(PositionProperty); }
            set { SetValue(PositionProperty, value); }
        }

        public static readonly DependencyProperty PositionProperty =
            DependencyProperty.Register("Position", typeof(TimeSpan), typeof(LyricControl), new PropertyMetadata(TimeSpan.Zero, (s, a) =>
            {
                if (s is LyricControl sender && !Equals(a.NewValue, a.OldValue))
                {
                    if (a.NewValue is TimeSpan newValue)
                    {
                        lock (sender.lyricLines.DrawingLocker)
                        {
                            sender.lyricLines.Position = newValue;
                            sender.lastPosition = newValue;
                            sender.totalTimeOffset = sender.totalTime;
                        }

                        sender.DrawOneFrameWhenPaused();
                    }
                }
            }));



        public bool Paused
        {
            get { return (bool)GetValue(PausedProperty); }
            set { SetValue(PausedProperty, value); }
        }

        public static readonly DependencyProperty PausedProperty =
            DependencyProperty.Register("Paused", typeof(bool), typeof(LyricControl), new PropertyMetadata(false));



        public bool LowFrameRateMode
        {
            get { return (bool)GetValue(LowFrameRateModeProperty); }
            set { SetValue(LowFrameRateModeProperty, value); }
        }

        // Using a DependencyProperty as the backing store for LowFrameRateMode.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty LowFrameRateModeProperty =
            DependencyProperty.Register("LowFrameRateMode", typeof(bool), typeof(LyricControl), new PropertyMetadata(false));



        public LyricControlTextStrokeType TextStrokeType
        {
            get { return (LyricControlTextStrokeType)GetValue(TextStrokeTypeProperty); }
            set { SetValue(TextStrokeTypeProperty, value); }
        }

        public static readonly DependencyProperty TextStrokeTypeProperty =
            DependencyProperty.Register("TextStrokeType", typeof(LyricControlTextStrokeType), typeof(LyricControl), new PropertyMetadata(LyricControlTextStrokeType.Auto));




        public LyricControlProgressAnimationMode ProgressAnimationMode
        {
            get { return (LyricControlProgressAnimationMode)GetValue(ProgressAnimationModeProperty); }
            set { SetValue(ProgressAnimationModeProperty, value); }
        }

        public static readonly DependencyProperty ProgressAnimationModeProperty =
            DependencyProperty.Register("ProgressAnimationMode", typeof(LyricControlProgressAnimationMode), typeof(LyricControl), new PropertyMetadata(LyricControlProgressAnimationMode.Karaoke));



        public LyricControlScrollAnimationMode ScrollAnimationMode
        {
            get { return (LyricControlScrollAnimationMode)GetValue(ScrollAnimationModeProperty); }
            set { SetValue(ScrollAnimationModeProperty, value); }
        }

        public static readonly DependencyProperty ScrollAnimationModeProperty =
            DependencyProperty.Register("ScrollAnimationMode", typeof(LyricControlScrollAnimationMode), typeof(LyricControl), new PropertyMetadata(LyricControlScrollAnimationMode.Slow));



        public LyricThemeView? Theme
        {
            get { return (LyricThemeView)GetValue(ThemeProperty); }
            set { SetValue(ThemeProperty, value); }
        }

        public static readonly DependencyProperty ThemeProperty =
            DependencyProperty.Register("Theme", typeof(LyricThemeView), typeof(LyricControlScrollAnimationMode), new PropertyMetadata(null));



        public string? LyricFontFamily
        {
            get { return (string)GetValue(LyricFontFamilyProperty); }
            set { SetValue(LyricFontFamilyProperty, value); }
        }

        public static readonly DependencyProperty LyricFontFamilyProperty =
            DependencyProperty.Register("LyricFontFamily", typeof(string), typeof(LyricThemeView), new PropertyMetadata(null));



        public bool IsLyricTranslateEnabled
        {
            get { return (bool)GetValue(IsLyricTranslateEnabledProperty); }
            set { SetValue(IsLyricTranslateEnabledProperty, value); }
        }

        public static readonly DependencyProperty IsLyricTranslateEnabledProperty =
            DependencyProperty.Register("IsLyricTranslateEnabled", typeof(bool), typeof(LyricThemeView), new PropertyMetadata(false));



        public TimeSpan MediaDuration
        {
            get { return (TimeSpan)GetValue(MediaDurationProperty); }
            set { SetValue(MediaDurationProperty, value); }
        }

        public static readonly DependencyProperty MediaDurationProperty =
            DependencyProperty.Register("MediaDuration", typeof(TimeSpan), typeof(LyricThemeView), new PropertyMetadata(TimeSpan.Zero));



        public bool TextOpacityMask
        {
            get { return (bool)GetValue(TextOpacityMaskProperty); }
            set { SetValue(TextOpacityMaskProperty, value); }
        }

        public static readonly DependencyProperty TextOpacityMaskProperty =
            DependencyProperty.Register("TextOpacityMask", typeof(bool), typeof(LyricThemeView), new PropertyMetadata(true));



        private void LyricControl_Loaded(object sender, RoutedEventArgs e)
        {
            CanvasControl.Update += CanvasControl_Update;
            CanvasControl.Draw += CanvasControl_Draw;

            CanvasControl.Paused = false;
        }

        private void LyricControl_Unloaded(object sender, RoutedEventArgs e)
        {
            CanvasControl.Update -= CanvasControl_Update;
            CanvasControl.Draw -= CanvasControl_Draw;

            CanvasControl.Paused = true;
        }

        private void LyricControl_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            DrawOneFrameWhenPaused();
        }

        private void Refresh()
        {
            lock (lyricLines.DrawingLocker)
            {
                drawingMainLine = null;
                drawingSecondaryLine = null;
                drawingRemovingLine = null;
            }

            DrawOneFrameWhenPaused();
        }

        private TimeSpan UpdateTotalTimeAndGetCurrentTime(TimeSpan totalTime)
        {
            var paused = propObserver[PausedProperty]!.GetValueOrDefault<bool>();

            var diff = this.totalTime - totalTimeOffset;

            this.totalTime = totalTime;
            if (paused)
            {
                totalTimeOffset = this.totalTime - diff;
            }

            return totalTime - totalTimeOffset + lastPosition;
        }

        private void DrawOneFrameWhenPaused()
        {
            if (IsLoaded)
            {
                CanvasControl.Paused = false;
            }
        }

        private void CanvasControl_Update(ICanvasAnimatedControl sender, CanvasAnimatedUpdateEventArgs args)
        {
            lock (lyricLines.DrawingLocker)
            {
                sender.Device.MaximumCacheSize = 10 * 1000 * 1000;

                var curTime = UpdateTotalTimeAndGetCurrentTime(args.Timing.TotalTime);

                lyricLines.Position = curTime;

                var lyric = propObserver[LyricProperty]!.GetValueOrDefault<Lyric>();
                if (lyric == null) return;

                var lineMode = propObserver[LineModeProperty]!.GetValueOrDefault<LyricControlLineMode>();
                var padding = propObserver[PaddingProperty]!.GetValueOrDefault<Thickness>();
                var lineSpace = propObserver[LineSpaceProperty]!.GetValueOrDefault<double>();
                var lineAlignment = propObserver[LineAlignmentProperty]!.GetValueOrDefault<LyricDrawingLineAlignment>();
                var lowFrameRateMode = propObserver[LowFrameRateModeProperty]!.GetValueOrDefault<bool>();

                var textStrokeType = propObserver[TextStrokeTypeProperty]!.GetValueOrDefault<LyricControlTextStrokeType>();
                var progressAnimationMode = propObserver[ProgressAnimationModeProperty]!.GetValueOrDefault<LyricControlProgressAnimationMode>();
                var theme = propObserver[ThemeProperty]!.GetValueOrDefault<LyricThemeView>();
                var fontFamily = propObserver[LyricFontFamilyProperty]!.GetValueOrDefault<string>();

                if (string.IsNullOrEmpty(fontFamily))
                {
                    fontFamily = GetDefaultFontFamilyName();
                }

                var controlSize = sender.Size;

                var width = controlSize.Width - padding.Left - padding.Right;
                var height = controlSize.Height - padding.Top - padding.Bottom;

                if (width <= 0 || height <= 0)
                {
                    drawingMainLine = null;
                    drawingSecondaryLine = null;
                    drawingRemovingLine = null;
                    return;
                }

                var curLineHeight = 0d;
                var nextLineHeight = 0d;
                var prevLineHeight = 0d;

                if (lineMode == LyricControlLineMode.DoubleLine)
                {
                    curLineHeight = (height - lineSpace) / (1 + LyricDrawingLine.ShowInitScale);
                    nextLineHeight = (height - lineSpace) - curLineHeight;
                }
                else
                {
                    curLineHeight = height;
                }
                prevLineHeight = curLineHeight;

                var mainLineSize = new Size(width, curLineHeight);
                var secondaryLineSize = new Size(width, nextLineHeight);
                var removingLineSize = new Size(width, prevLineHeight);

                var strokeWidth = 1f;
                bool strokeFlag = false;

                if (textStrokeType == LyricControlTextStrokeType.Enabled) strokeFlag = true;
                else if (textStrokeType == LyricControlTextStrokeType.Auto && height > 100) strokeFlag = true;

                colors ??= CreateDefaultColors();

                bool karaokeFlag = progressAnimationMode == LyricControlProgressAnimationMode.Karaoke;

                colors.FillColor1 = themeColors.FillColor1;
                colors.StrokeColor1 = themeColors.StrokeColor1;
                colors.FillColor2 = karaokeFlag ? themeColors.FillColor2 : themeColors.FillColor1;
                colors.StrokeColor2 = karaokeFlag ? themeColors.StrokeColor2 : themeColors.StrokeColor1;

                if (strokeFlag)
                {
                    if (height < 60)
                    {
                        strokeWidth = 60f / 160;
                    }
                    else if (height < 160)
                    {
                        strokeWidth = (float)(height / 160);
                    }
                    else
                    {
                        strokeWidth = 1f;
                    }

                    colors.GlowColor1 = Color.FromArgb((byte)(0.1 * 255), 0, 0, 0);
                    colors.GlowColor2 = Color.FromArgb((byte)(0.25 * 255), 0, 0, 0);
                }
                else
                {
                    strokeWidth = 0f;

                    colors.GlowColor1 = Color.FromArgb((byte)(0.75 * 255), 0, 0, 0);
                    colors.GlowColor2 = Color.FromArgb((byte)(0.9 * 255), 0, 0, 0);
                }

                var sizeType = LyricDrawingLineTextSizeType.LayoutSize;

                if (lyricLines.MainLine != drawingMainLine?.LyricLine || drawingMainLine?.Size != mainLineSize)
                {
                    drawingMainLine?.Dispose();
                    drawingMainLine = null;

                    if (lyricLines.MainLine != null)
                    {
                        drawingMainLine = new LyricDrawingLine(
                            sender,
                            mainLineSize,
                            lyricLines.MainLine,
                            fontFamily,
                            LyricDrawingLineType.Classic,
                            lineAlignment,
                            colors,
                            strokeWidth,
                            sizeType,
                            progressAnimationMode);
                    }

                    try
                    {
                        sender.Device.Trim();
                    }
                    catch (Exception ex) when (!sender.Device.IsDeviceLost(ex.HResult)) { }
                }


                if (lyricLines.SecondaryLine != drawingSecondaryLine?.LyricLine || drawingSecondaryLine?.Size != secondaryLineSize)
                {
                    drawingSecondaryLine?.Dispose();
                    drawingSecondaryLine = null;

                    if (lyricLines.SecondaryLine != null)
                    {
                        drawingSecondaryLine = new LyricDrawingLine(
                            sender,
                            secondaryLineSize,
                            lyricLines.SecondaryLine,
                            fontFamily,
                            LyricDrawingLineType.Classic,
                            lineAlignment,
                            colors,
                            strokeWidth,
                            sizeType,
                            progressAnimationMode);
                    }
                }


                if (lyricLines.RemovingLine != drawingRemovingLine?.LyricLine || drawingRemovingLine?.Size != removingLineSize)
                {
                    drawingRemovingLine?.Dispose();
                    drawingRemovingLine = null;

                    if (lyricLines.RemovingLine != null)
                    {
                        drawingRemovingLine = new LyricDrawingLine(
                            sender,
                            removingLineSize,
                            lyricLines.RemovingLine,
                            fontFamily,
                            LyricDrawingLineType.Classic,
                            lineAlignment,
                            colors,
                            strokeWidth,
                            sizeType,
                            progressAnimationMode);
                    }
                }
            }
        }

        private void CanvasControl_Draw(ICanvasAnimatedControl sender, CanvasAnimatedDrawEventArgs args)
        {
            LyricDrawingLine? drawingLine = null;
            LyricDrawingLine? drawingNextLine = null;
            Matrix3x2 matrix = Matrix3x2.Identity;

            LyricControlLineMode lineMode;
            Thickness padding;
            double opacity;
            double lineSpace;
            TimeSpan curTime;
            bool lowFrameRateMode;
            bool clipToPadding;
            bool paused;
            LyricControlProgressAnimationMode progressAnimationMode;
            LyricControlScrollAnimationMode scrollAnimationMode;
            TimeSpan mediaDuration;
            bool textOpacityMask;


            lock (lyricLines.DrawingLocker)
            {
                drawingLine = this.drawingMainLine;
                drawingNextLine = this.drawingSecondaryLine;
                lineMode = propObserver[LineModeProperty]!.GetValueOrDefault<LyricControlLineMode>();
                padding = propObserver[PaddingProperty]!.GetValueOrDefault<Thickness>();
                opacity = propObserver[OpacityProperty]!.GetValueOrDefault<double>(1);
                lineSpace = propObserver[LineSpaceProperty]!.GetValueOrDefault<double>();
                clipToPadding = propObserver[ClipToPaddingProperty]!.GetValueOrDefault<bool>();
                lowFrameRateMode = propObserver[LowFrameRateModeProperty]!.GetValueOrDefault<bool>();
                paused = propObserver[PausedProperty]!.GetValueOrDefault<bool>();

                progressAnimationMode = propObserver[ProgressAnimationModeProperty]!.GetValueOrDefault<LyricControlProgressAnimationMode>();
                scrollAnimationMode = propObserver[ScrollAnimationModeProperty]!.GetValueOrDefault<LyricControlScrollAnimationMode>();
                mediaDuration = propObserver[MediaDurationProperty]!.GetValueOrDefault<TimeSpan>();

                textOpacityMask = propObserver[TextOpacityMaskProperty]!.GetValueOrDefault<bool>();

                matrix = Matrix3x2.CreateTranslation((float)padding.Left, (float)padding.Top);

                curTime = UpdateTotalTimeAndGetCurrentTime(args.Timing.TotalTime);
            }

            bool karaokeFlag = progressAnimationMode == LyricControlProgressAnimationMode.Karaoke;

            if (drawingLine != null)
            {
                CanvasActiveLayer? layer = null;
                CanvasImageBrush? brush = null;

                try
                {
                    if (textOpacityMask && !lowFrameRateMode)
                    {
                        var opacityBorderAmount = 0.06f;
                        var effect = new VignetteEffect()
                        {
                            Amount = opacityBorderAmount,
                            Curve = 1f,
                            Color = Color.FromArgb(0, 0, 0, 0),
                            Source = new CropEffect()
                            {
                                SourceRectangle = new Rect(
                                    0,
                                    -sender.Size.Height * opacityBorderAmount,
                                    sender.Size.Width,
                                    sender.Size.Height * (1 + opacityBorderAmount * 2)),
                                Source = new ColorSourceEffect()
                                {
                                    Color = Color.FromArgb(
                                        a: Math.Clamp((byte)(255 * opacity), (byte)0, (byte)255),
                                        r: 0,
                                        g: 0,
                                        b: 0)
                                }
                            }
                        };
                        brush = new CanvasImageBrush(args.DrawingSession, effect)
                        {
                            SourceRectangle = new Rect(default, sender.Size)
                        };
                        layer = args.DrawingSession.CreateLayer(brush);
                    }
                    else
                    {
                        layer = args.DrawingSession.CreateLayer((float)opacity);
                    }

                    try
                    {
                        var endTime = drawingLine.LyricLine.EndTime;
                        if (drawingLine.LyricLine.IsEndLine
                            && mediaDuration > drawingLine.LyricLine.StartTime
                            && endTime >= TimeSpan.FromDays(100))
                        {
                            endTime = mediaDuration;
                        }

                        GetProgress(curTime,
                            drawingLine.LyricLine.StartTime,
                            endTime,
                            drawingRemovingLine?.LyricLine.EndTime,
                            scrollAnimationMode,
                            out var playProgress,
                            out var scaleProgress);

                        var originalScaleProgress = scaleProgress;

                        // scaleProgress: 0 - 1
                        scaleProgress = easingFunc.Ease(scaleProgress);
                        var moveProgress = 1 - scaleProgress;
                        var nextLinePlayProgress = 0d;

                        if (lyricLines.SecondaryLineIsTranslate || lowFrameRateMode)
                        {
                            scaleProgress = 1;
                            moveProgress = 0;

                            if (lyricLines.SecondaryLineIsTranslate)
                            {
                                nextLinePlayProgress = playProgress;
                            }
                        }

                        if (drawingRemovingLine != null && !lowFrameRateMode && 1 - scaleProgress > 0)
                        {
                            CanvasActiveLayer? removingLineLayer = null;

                            try
                            {
                                Rect? clipRect = null;

                                if (clipToPadding)
                                {
                                    args.DrawingSession.Transform = Matrix3x2.Identity;
                                    var left = padding.Left;
                                    var top = padding.Top;
                                    var width = sender.Size.Width - padding.Left - padding.Right;
                                    var height = sender.Size.Height - padding.Top - padding.Bottom;

                                    if (width <= 0 || height <= 0)
                                    {
                                        width = 0;
                                        height = 0;
                                    }

                                    clipRect = new Rect(left, top, width, height);
                                }

                                var opacityProgress = (float)easingFunc.Ease(1 - originalScaleProgress);

                                if (clipRect.HasValue)
                                {
                                    if (!clipRect.Value.IsEmpty)
                                    {
                                        removingLineLayer = args.DrawingSession.CreateLayer(opacityProgress, clipRect.Value);
                                    }
                                }
                                else
                                {
                                    removingLineLayer = args.DrawingSession.CreateLayer(opacityProgress);
                                }

                                if (!clipRect.HasValue || (clipRect.HasValue && !clipRect.Value.IsEmpty))
                                {
                                    using (var opacityLayer = args.DrawingSession.CreateLayer((float)(1 - scaleProgress)))
                                    {
                                        args.DrawingSession.Transform = matrix
                                            * Matrix3x2.CreateTranslation(0, (float)(-drawingRemovingLine.Size.Height * LyricDrawingLine.HideFinalScale - lineSpace))
                                            * Matrix3x2.CreateTranslation(0, (float)(moveProgress * (drawingRemovingLine.Size.Height * LyricDrawingLine.HideFinalScale + lineSpace)));

                                        drawingRemovingLine?.Draw(args.DrawingSession, 1, 1 - scaleProgress, lowFrameRateMode);
                                    }
                                }
                            }
                            finally
                            {
                                removingLineLayer?.Dispose();
                            }
                        }

                        if (lineMode == LyricControlLineMode.DoubleLine && drawingNextLine != null)
                        {
                            using (var opacityLayer = args.DrawingSession.CreateLayer((float)scaleProgress))
                            {
                                args.DrawingSession.Transform = matrix
                                    * Matrix3x2.CreateTranslation(0, (float)(drawingLine.Size.Height + lineSpace))
                                    * Matrix3x2.CreateTranslation(0, (float)(moveProgress * (drawingNextLine.Size.Height + lineSpace)));

                                drawingNextLine?.Draw(args.DrawingSession, nextLinePlayProgress, 1, lowFrameRateMode);
                            }
                        }

                        args.DrawingSession.Transform = matrix
                            * Matrix3x2.CreateTranslation(0, (float)(moveProgress * (drawingLine.Size.Height + lineSpace)));

                        drawingLine.Draw(args.DrawingSession, playProgress, scaleProgress, lowFrameRateMode);

                    }
                    catch (Exception ex) when (!sender.Device.IsDeviceLost(ex.HResult)) { }
                    finally
                    {
                        args.DrawingSession.Transform = Matrix3x2.Identity;
                    }
                }
                finally
                {
                    layer?.Dispose();
                    brush?.Dispose();

                    if (paused)
                    {
                        CanvasControl.Paused = paused;
                    }
                }
            }
        }

        private static void GetProgress(TimeSpan time, TimeSpan startTime, TimeSpan endTime, TimeSpan? prevLineEndTime, LyricControlScrollAnimationMode scrollAnimationMode, out double playProgress, out double scaleProgress)
        {
            const double FastScaleSeconds = 0.4d;
            const double SlowScaleSeconds = 0.8d;

            var scaleSeconds = 0d;
            if (scrollAnimationMode == LyricControlScrollAnimationMode.Slow) scaleSeconds = SlowScaleSeconds;
            else if (scrollAnimationMode == LyricControlScrollAnimationMode.Fast) scaleSeconds = FastScaleSeconds;

            scaleProgress = 0;

            if (scaleSeconds > 0)
            {
                if (prevLineEndTime.HasValue)
                {
                    if (time <= prevLineEndTime.Value)
                    {
                        scaleProgress = 0;
                    }
                    else if (time >= endTime)
                    {
                        scaleProgress = 1;
                    }
                    else
                    {
                        var duration = endTime - prevLineEndTime.Value;
                        var scaleRatio = duration.TotalSeconds > scaleSeconds ? scaleSeconds / duration.TotalSeconds : 0;

                        var progress = (time.TotalSeconds - prevLineEndTime.Value.TotalSeconds) / duration.TotalSeconds;

                        scaleProgress = scaleRatio == 0 ? 0 : Math.Min(1, Math.Max(0, progress / scaleRatio));
                    }
                }
            }
            else
            {
                scaleProgress = 1;
            }

            if (time <= startTime)
            {
                playProgress = 0;

                if (!prevLineEndTime.HasValue)
                {
                    scaleProgress = 0;
                }
            }
            else if (time >= endTime)
            {
                playProgress = 1;

                if (!prevLineEndTime.HasValue)
                {
                    scaleProgress = 1;
                }
            }
            else
            {
                var duration = endTime - startTime;
                var scaleRatio = duration.TotalSeconds > scaleSeconds ? scaleSeconds / duration.TotalSeconds : 0;

                var progress = (time.TotalSeconds - startTime.TotalSeconds) / duration.TotalSeconds;

                playProgress = progress;

                if (!prevLineEndTime.HasValue && scaleSeconds > 0)
                {
                    scaleProgress = scaleRatio == 0 ? 0 : Math.Min(1, Math.Max(0, progress / scaleRatio));
                }
            }
        }

        private static LyricDrawingTextColors CreateDefaultColors() =>
            new LyricDrawingTextColors()
            {
                FillColor1 = Colors.White,
                StrokeColor1 = Colors.Black,
                GlowColor1 = Color.FromArgb((byte)(0.1 * 255), 0, 0, 0),

                FillColor2 = Color.FromArgb(255, 255, 160, 77),
                StrokeColor2 = Colors.Black,
                GlowColor2 = Color.FromArgb((byte)(0.25 * 255), 0, 0, 0),
            };

        private static string? defaultFontFamilyName;

        private static string GetDefaultFontFamilyName()
        {
            if (!string.IsNullOrEmpty(defaultFontFamilyName)) return defaultFontFamilyName;

            var cultureName = CultureInfoUtils.DefaultUICulture.Name;
            if (string.IsNullOrEmpty(cultureName)) cultureName = "en-us";

            try
            {
                var fontGroup = new Windows.Globalization.Fonts.LanguageFontGroup(cultureName);
                var font = fontGroup.UITextFont.FontFamily;
                if (!string.IsNullOrEmpty(font)) return (defaultFontFamilyName = font);
            }
            catch (Exception ex)
            {
                NLog.LogManager.GetCurrentClassLogger().Error(ex, "GetDefaultFontFamilyName");
            }
            return (defaultFontFamilyName = "Segoe UI");
        }

        private class CircleEase
        {
            public EasingMode EasingMode { get; set; }

            private double EaseInCore(double normalizedTime)
            {
                normalizedTime = Math.Max(0.0, Math.Min(1.0, normalizedTime));
                return 1.0 - Math.Sqrt(1.0 - normalizedTime * normalizedTime);
            }

            public double Ease(double normalizedTime)
            {
                switch (EasingMode)
                {
                    case EasingMode.EaseIn:
                        return EaseInCore(normalizedTime);
                    case EasingMode.EaseOut:
                        // EaseOut is the same as EaseIn, except time is reversed & the result is flipped.
                        return 1.0 - EaseInCore(1.0 - normalizedTime);
                    case EasingMode.EaseInOut:
                    default:
                        // EaseInOut is a combination of EaseIn & EaseOut fit to the 0-1, 0-1 range.
                        return (normalizedTime < 0.5) ?
                                   EaseInCore(normalizedTime * 2.0) * 0.5 :
                            (1.0 - EaseInCore((1.0 - normalizedTime) * 2.0)) * 0.5 + 0.5;
                }
            }
        }
    }

    public enum LyricControlLineMode
    {
        SingleLine,
        DoubleLine
    }

    public enum LyricControlTextStrokeType
    {
        Auto,
        Enabled,
        Disabled
    }

    public enum LyricControlProgressAnimationMode
    {
        Disabled,
        Karaoke
    }

    public enum LyricControlScrollAnimationMode
    {
        Disabled,
        Fast,
        Slow
    }
}
