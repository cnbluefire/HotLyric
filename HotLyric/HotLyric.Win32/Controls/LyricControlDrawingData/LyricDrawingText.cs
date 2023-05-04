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
        public void Draw(CanvasDrawingSession drawingSession, in LyricDrawingParameters parameters)
        {
            DrawCore(drawingSession, in parameters);
        }

        protected virtual void DrawCore(CanvasDrawingSession drawingSession, in LyricDrawingParameters parameters) { }

    }
}
