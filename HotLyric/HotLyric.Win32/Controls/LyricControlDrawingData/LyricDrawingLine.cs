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

namespace HotLyric.Win32.Controls.LyricControlDrawingData
{
    internal class LyricDrawingLine : LyricDrawingElement
    {
        public const double HideFinalScale = 1d;
        public const double ShowInitScale = 0.8d;

        private readonly ICanvasResourceCreator resourceCreator;

        private LyricDrawingTextGlyphRunGroup glyphRunGroup;
        private List<LyricDrawingText>? lyricTexts;

        public LyricDrawingLine(
            ICanvasResourceCreator resourceCreator,
            Size size,
            ILyricLine line,
            string fontFamily,
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
            FontFamily = fontFamily;
            Type = type;
            Alignment = alignment;
            glyphRunGroup = LyricDrawingTextGlyphRunGroup.Create(resourceCreator, line.Text, FontFamily);
            CreateLyricText();
        }

        public Size TextSize => TextSizeType switch
        {
            LyricDrawingLineTextSizeType.DrawSize => glyphRunGroup.TextDrawSize,
            LyricDrawingLineTextSizeType.LayoutSize => glyphRunGroup.TextLayoutSize,
            _ => throw new ArgumentException(nameof(TextSizeType))
        };

        public LyricDrawingLineTextSizeType TextSizeType { get; }

        public Size Size { get; }

        public ILyricLine LyricLine { get; }

        public string FontFamily { get; }

        public LyricDrawingLineType Type { get; }

        public LyricDrawingLineAlignment Alignment { get; }

        public IReadOnlyList<LyricDrawingText> LyricTexts => lyricTexts!;

        public float StrokeWidth { get; }

        private void CreateLyricText()
        {
            lyricTexts = new List<LyricDrawingText>();

            if (glyphRunGroup.GlyphRuns.Count > 0)
            {
                var geometryScale = TextSize.Height == 0 ? 0 : Size.Height / TextSize.Height;

                if (Type == LyricDrawingLineType.Classic)
                {
                    lyricTexts.Add(new LyricDrawingTextClassic(resourceCreator, glyphRunGroup.GlyphRuns, StrokeWidth, geometryScale, TextSizeType));

                }
                else
                {
                    throw new NotSupportedException(Type.ToString());
                }
            }
        }

        public void Draw(CanvasDrawingSession drawingSession, in LyricDrawingParameters parameters)
        {
            if (glyphRunGroup.GlyphRuns.Count == 0) return;

            var scaleProgress = parameters.ScaleProgress;
            var playProgress = parameters.PlayProgress;

            if (scaleProgress > 1 || scaleProgress < 0)
            {
                throw new ArgumentException(nameof(scaleProgress));
            }

            var geometryScale = TextSize.Height == 0 ? 0 : Size.Height / TextSize.Height;

            var textWidth = TextSize.Width * geometryScale;

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

            drawingSession.Transform = matrix;

            foreach (var lyricText in lyricTexts!)
            {
                lyricText.Draw(drawingSession, in parameters);
            }

            drawingSession.Transform = oldMatrix;
        }

        protected override void DisposeCore(bool disposing)
        {
            glyphRunGroup?.Dispose();
            glyphRunGroup = null!;

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
        DrawSize
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
