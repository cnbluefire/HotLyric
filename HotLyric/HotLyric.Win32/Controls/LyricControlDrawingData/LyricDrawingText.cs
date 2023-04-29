using Microsoft.Graphics.Canvas;
using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace HotLyric.Win32.Controls.LyricControlDrawingData
{
    internal abstract class LyricDrawingText : LyricDrawingElement
    {
        public double ProgressRangeStart { get; set; }

        public double ProgressRangeEnd { get; set; }

        public void Draw(CanvasDrawingSession drawingSession, double progress, bool lowFrameRateMode)
        {
            DrawCore(drawingSession, MapProgress(progress), lowFrameRateMode);
        }

        protected virtual void DrawCore(CanvasDrawingSession drawingSession, double progress, bool lowFrameRateMode) { }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private double MapProgress(double progress)
        {
            var rangeStart = ProgressRangeStart;
            var rangeEnd = ProgressRangeEnd;

            return Math.Max(0, Math.Min(1, (progress - rangeStart) / (rangeEnd - rangeStart)));
        }

    }
}
