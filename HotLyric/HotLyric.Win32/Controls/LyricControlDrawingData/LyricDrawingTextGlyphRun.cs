using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI.Composition;

namespace HotLyric.Win32.Controls.LyricControlDrawingData
{
    public class LyricDrawingTextGlyphRun
    {
        public Vector2 Point { get; set; }

        public CanvasFontFace? FontFace { get; set; }

        public float FontSize { get; set; }

        public CanvasGlyph[]? Glyphs { get; set; }

        public bool IsSideways { get; set; }

        public uint BidiLevel { get; set; }

        public string? LocaleName { get; set; }

        public string? TextString { get; set; }

        public uint CharacterIndex { get; set; }

        public CanvasGlyphOrientation GlyphOrientation { get; set; }

        public bool IsEmoji { get; set; }

        public override string? ToString()
        {
            return TextString;
        }
    }
}
