using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Composition;
using Microsoft.Graphics.Canvas;
using Windows.UI;
using Windows.Foundation;
using System.Runtime.CompilerServices;
using System.Numerics;
using HotLyric.Win32.Utils.LyricFiles;
using BlueFire.Toolkit.WinUI3.Text;
using HotLyric.Win32.Models;
using HotLyric.Win32.Utils;
using Windows.UI.Text;

namespace HotLyric.Win32.Controls.LyricControlDrawingData
{
    internal class LyricDrawingLine : LyricDrawingElement
    {
        public const double HideFinalScale = 1d;
        public const double ShowInitScale = 0.8d;

        private readonly ICanvasResourceCreator resourceCreator;

        private List<LyricDrawingText>? lyricTexts;
        private FormattedText formattedText;

        public LyricDrawingLine(
            ICanvasResourceCreator resourceCreator,
            Size size,
            ILyricLine line,
            FontFamilySets fontFamilies,
            Windows.UI.Text.FontWeight fontWeight,
            Windows.UI.Text.FontStyle fontStyle,
            LyricDrawingLineType type,
            LyricDrawingLineAlignment alignment,
            float strokeWidth,
            LyricDrawingLineTextSizeType textSizeType)
        {
            this.resourceCreator = resourceCreator;
            StrokeWidth = strokeWidth;
            TextSizeType = textSizeType;
            Size = size;
            LyricLine = line;
            FontFamilies = fontFamilies;
            Type = type;
            Alignment = alignment;
            formattedText = CreateFormattedText(line.Text, fontFamilies, fontWeight, fontStyle);
            CreateLyricText();
            FontWeight = fontWeight;
            FontStyle = fontStyle;
        }

        public Size TextSize => TextSizeType switch
        {
            LyricDrawingLineTextSizeType.DrawSize => new Size(formattedText.Width, Math.Max(formattedText.Extent, 0)),
            LyricDrawingLineTextSizeType.LayoutSize => new Size(formattedText.Width, formattedText.Height),
            LyricDrawingLineTextSizeType.FontHeight => GetTextSizeByFontHeight(),
            _ => throw new ArgumentException(nameof(TextSizeType))
        };

        public LyricDrawingLineTextSizeType TextSizeType { get; }

        public Size Size { get; }

        public ILyricLine LyricLine { get; }

        public FontFamilySets FontFamilies { get; }

        public Windows.UI.Text.FontWeight FontWeight { get; }

        public Windows.UI.Text.FontStyle FontStyle { get; }

        public LyricDrawingLineType Type { get; }

        public LyricDrawingLineAlignment Alignment { get; }

        public IReadOnlyList<LyricDrawingText> LyricTexts => lyricTexts!;

        public float StrokeWidth { get; }

        private void CreateLyricText()
        {
            lyricTexts = new List<LyricDrawingText>();

            if (formattedText.LineGlyphRuns.Sum(c => c.GlyphRuns.Length) > 0)
            {
                var geometryScale = TextSize.Height == 0 ? 0 : Size.Height / TextSize.Height;

                if (Type == LyricDrawingLineType.Classic)
                {
                    lyricTexts.Add(new LyricDrawingTextClassic(resourceCreator, formattedText, StrokeWidth, geometryScale, TextSizeType));

                }
                else
                {
                    throw new NotSupportedException(Type.ToString());
                }
            }
        }

        public void Draw(CanvasDrawingSession drawingSession, in LyricDrawingParameters parameters)
        {
            if (formattedText.LineGlyphRuns.Count == 0) return;

            var scaleProgress = parameters.ScaleProgress;
            var playProgress = parameters.PlayProgress;

            if (scaleProgress > 1 || scaleProgress < 0)
            {
                throw new ArgumentException(nameof(scaleProgress));
            }

            var geometryScale = TextSize.Height == 0 ? 0 : Size.Height / TextSize.Height;

            var textWidth = TextSize.Width * geometryScale;
            var textHeight = TextSize.Height * geometryScale;

            var scale = playProgress switch
            {
                1 => (1 - HideFinalScale) * scaleProgress + HideFinalScale,
                _ => (1 - ShowInitScale) * scaleProgress + ShowInitScale,
            };

            var textActualWidth = textWidth * scale;

            var matrix = drawingSession.Transform;
            var oldMatrix = matrix;

            matrix = Matrix3x2.CreateScale((float)scale) * matrix;

            var offset = 0d;

            if (Size.Width >= textWidth)
            {
                offset = Alignment switch
                {
                    LyricDrawingLineAlignment.Left => 0d,
                    LyricDrawingLineAlignment.Center => (Size.Width - textActualWidth) / 2,
                    LyricDrawingLineAlignment.Right => Size.Width - textActualWidth,
                    _ => throw new ArgumentException(nameof(Alignment))
                };
            }
            else
            {
                // 位移补偿
                var biasValue = Math.Min(1, Math.Max(0, (textWidth / Size.Width) / 3 * 0.15 + 1));
                var scrollOffset = -(textWidth - Size.Width) * Math.Min(1, Math.Max(0, (playProgress * biasValue)));

                if (scaleProgress != 1)
                {
                    var finalScale = playProgress == 1 ? HideFinalScale : ShowInitScale;
                    var textFinalWidth = textWidth * finalScale;

                    if (Size.Width < textFinalWidth)
                    {
                        offset = playProgress switch
                        {
                            1 => -(textActualWidth - Size.Width),
                            _ => 0,
                        };
                    }
                    else
                    {
                        if (playProgress == 1)
                        {
                            offset = Alignment switch
                            {
                                LyricDrawingLineAlignment.Left => -(textWidth - Size.Width) * scaleProgress,
                                LyricDrawingLineAlignment.Center => -(textWidth - Size.Width) + ((Size.Width - textFinalWidth) / 2 + (textWidth - Size.Width)) * (1 - scaleProgress),
                                LyricDrawingLineAlignment.Right => -(textWidth - Size.Width) + (textWidth - textFinalWidth) * (1 - scaleProgress),
                                _ => throw new ArgumentException(nameof(Alignment))
                            };
                        }
                        else
                        {
                            offset = Alignment switch
                            {
                                LyricDrawingLineAlignment.Left => 0d,
                                LyricDrawingLineAlignment.Center => (Size.Width - textFinalWidth) / 2 * (1 - scaleProgress),
                                LyricDrawingLineAlignment.Right => (Size.Width - textFinalWidth) * (1 - scaleProgress),
                                _ => throw new ArgumentException(nameof(Alignment))
                            };
                        }
                    }
                }

                if (playProgress != 1)
                {
                    offset += scrollOffset;
                }
            }

            matrix *= Matrix3x2.CreateTranslation((float)offset, 0);

            if (TextSizeType == LyricDrawingLineTextSizeType.FontHeight)
            {
                var actualLineHeight = formattedText.Height * geometryScale;
                matrix *= Matrix3x2.CreateTranslation(0, (float)(textHeight - actualLineHeight) / 2);
            }

            drawingSession.Transform = matrix;

            foreach (var lyricText in lyricTexts!)
            {
                lyricText.Draw(drawingSession, in parameters);
            }

            drawingSession.Transform = oldMatrix;
        }

        private Size GetTextSizeByFontHeight()
        {
            var width = formattedText.Width;
            var height = formattedText.Height;

            var prop = SystemFontHelper.GetFontProperties(FontFamilies.PrimaryFontFamily, CultureInfoUtils.DefaultUICulture.Name);

            if (prop != null)
            {
                height = (prop.Ascent + prop.Descent + prop.LineGap) * 10;
            }

            return new Size(width, height);
        }

        private static FormattedText CreateFormattedText(string textString, FontFamilySets fontFamilies, FontWeight fontWeight, FontStyle fontStyle)
        {
            var fontFamily = fontFamilies.PrimaryFontFamily;

            if (fontFamilies.IsCompositeFont)
            {
                fontFamily = FontFamilySets.LyricCompositeFontFamilyName;
            }

            return new FormattedText(
                textString,
                "en",
                Microsoft.UI.Xaml.FlowDirection.LeftToRight,
                new FormattedTextTypeface(
                    fontFamily,
                    fontWeight,
                    fontStyle,
                    FontStretch.Normal),
                10,
                false,
                false,
                null);
        }

        protected override void DisposeCore(bool disposing)
        {
            formattedText?.Dispose();
            formattedText = null!;

            var list = lyricTexts;
            lyricTexts = null;

            if (list != null)
            {
                foreach (var item in list!)
                {
                    item.Dispose();
                }
                list = null;
            }
        }
    }

    internal enum LyricDrawingLineType
    {
        /// <summary>
        /// 经典卡拉OK模式
        /// </summary>
        Classic,

        /// <summary>
        /// 逐词模式
        /// </summary>
        ClipByWord,
    }

    public enum LyricDrawingLineAlignment
    {
        //
        // 摘要:
        //     An element aligned to the left of the layout slot for the parent element.
        Left,
        //
        // 摘要:
        //     An element aligned to the center of the layout slot for the parent element.
        Center,
        //
        // 摘要:
        //     An element aligned to the right of the layout slot for the parent element.
        Right
    }

    internal enum LyricDrawingLineTextSizeType
    {
        LayoutSize,
        DrawSize,
        FontHeight
    }

    internal struct LyricDrawingParameters
    {
        public LyricDrawingParameters(
            double playProgress,
            double scaleProgress,
            bool lowFrameRateMode,
            LyricControlProgressAnimationMode progressAnimationMode,
            LyricDrawingTextColors colors)
        {
            PlayProgress = playProgress;
            ScaleProgress = scaleProgress;
            LowFrameRateMode = lowFrameRateMode;
            ProgressAnimationMode = progressAnimationMode;
            Colors = colors;
        }

        public double PlayProgress { get; }

        public double ScaleProgress { get; }

        public bool LowFrameRateMode { get; }

        public LyricControlProgressAnimationMode ProgressAnimationMode { get; }

        public LyricDrawingTextColors Colors { get; }
    }
}
