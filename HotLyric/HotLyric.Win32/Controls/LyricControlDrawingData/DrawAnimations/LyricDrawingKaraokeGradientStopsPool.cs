using Microsoft.Graphics.Canvas.Brushes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HotLyric.Win32.Controls.LyricControlDrawingData.DrawAnimations
{
    internal static class LyricDrawingKaraokeGradientStopsPool
    {
        private const int ElementCount = 4;
        private const int MaxPoolCapacity = 40;

        private static Queue<CanvasGradientStop[]> stopsPool = new Queue<CanvasGradientStop[]>();

        public static GradientStopsHolder Rent()
        {
            CanvasGradientStop[]? stops;

            lock (stopsPool)
            {
                if (stopsPool.Count > 0) stops = stopsPool.Dequeue();
                else stops = new CanvasGradientStop[ElementCount];
            }

            return new GradientStopsHolder(stops);
        }

        public static void Return(CanvasGradientStop[] stops)
        {
            if (stops == null || stops.Length != ElementCount) return;

            lock (stopsPool)
            {
                if (stopsPool.Count > MaxPoolCapacity) return;
                stopsPool.Enqueue(stops);
            }
        }
    }

    internal struct GradientStopsHolder : IDisposable
    {
        public GradientStopsHolder(CanvasGradientStop[] stops)
        {
            Stops = stops;
        }

        public CanvasGradientStop[] Stops { get; }

        public void Dispose()
        {
            LyricDrawingKaraokeGradientStopsPool.Return(Stops);
        }
    }
}
