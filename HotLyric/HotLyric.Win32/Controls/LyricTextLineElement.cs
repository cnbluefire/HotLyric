//using Microsoft.Graphics.Canvas;
//using Microsoft.Graphics.Canvas.Text;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Numerics;
//using System.Text;
//using System.Threading.Tasks;
//using Windows.Foundation;
//using Windows.UI.Composition;

//namespace HotLyric.Win32.Controls
//{
//    public partial class LyricTextLineElement : IDisposable
//    {
//        private readonly Compositor compositor;
//        private readonly ContainerVisual rootVisual;
//        private Size size;
//        private LyricTextLineRenderer renderer;
//        private bool disposedValue;

//        public LyricTextLineElement(Compositor compositor, string text, ContainerVisual rootVisual, Size size)
//        {
//            this.compositor = compositor;
//            this.rootVisual = rootVisual;
//            this.size = size;
//            Text = text;

//            InitTextProperties();
//            ResizeCore();
//        }

//        public string Text { get; }

//        public void Resize(Size size)
//        {
//            if (this.size != size)
//            {
//                this.size = size;
//                ResizeCore();
//            }
//        }

//        private LyricTextElement element;

//        public Visual Visual => element.Visual;

//        private void InitTextProperties()
//        {
//            renderer = new LyricTextLineRenderer(CanvasDevice.GetSharedDevice(), Text, "Microsoft YaHei UI");
//            if (renderer.GlyphRuns.Count > 0)
//            {
//                element = new LyricClassicTextElement(compositor, renderer.GlyphRuns, renderer.TextLayoutSize);
//            }
//        }

//        private void ResizeCore()
//        {
//            if (this.size.IsEmpty) return;


//        }

//        protected virtual void Dispose(bool disposing)
//        {
//            if (!disposedValue)
//            {
//                if (disposing)
//                {

//                }

//                disposedValue = true;
//            }
//        }

//        public void Dispose()
//        {
//            Dispose(disposing: true);
//            GC.SuppressFinalize(this);
//        }

//    }
//}
