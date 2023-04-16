//using Microsoft.Graphics.Canvas.Effects;
//using Microsoft.Graphics.Canvas.Geometry;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Numerics;
//using System.Text;
//using System.Threading.Tasks;
//using Windows.Foundation;
//using Windows.UI;
//using Windows.UI.Composition;
//using Windows.UI.Xaml.Controls.Primitives;
//using Windows.UI.Xaml.Media;
//using Windows.UI.Xaml.Shapes;

//namespace HotLyric.Win32.Controls
//{
//    public abstract class LyricTextElement
//    {
//        public abstract Visual? Visual { get; }

//    }

//    public enum LyricTextElementType
//    {
//        Classic,        // 经典卡拉OK模式
//        ClipByWord,     // 逐词模式
//        ClipByCharater, // 逐字模式
//    }

//    public class LyricClipByCharaterTextElement : LyricTextElement
//    {
//        private List<Data> dataList;
//        private ShapeVisual? shapeVisual;
//        private ContainerVisual? containerVisual;

//        private class Data : IDisposable
//        {
//            private readonly Compositor compositor;
//            private CompositionPath? compositionPath;
//            private CompositionGeometry? compositionGeometry;
//            private CompositionSpriteShape? compositionShape;
//            private CompositionAnimation? animation;

//            public Data(Compositor compositor, CanvasGeometry geometry, CompositionBrush fillBrush)
//            {
//                this.compositor = compositor;

//                compositionPath = new CompositionPath(geometry.Transform(Matrix3x2.CreateTranslation(0, 10)));

//                compositionGeometry = compositor.CreatePathGeometry(compositionPath);

//                compositionShape = compositor.CreateSpriteShape(compositionGeometry);

//                compositionShape.StrokeThickness = 0.2f;
//                compositionShape.FillBrush = fillBrush;
//                compositionShape.StrokeBrush = compositor.CreateColorBrush(Colors.Black);
//            }

//            public CompositionSpriteShape? Shape => compositionShape;

//            public void StartAnimation(TimeSpan delayTime)
//            {
//                var lineFunc = compositor.CreateLinearEasingFunction();
//                var an = compositor.CreateVector2KeyFrameAnimation();
//                an.InsertKeyFrame(0, Vector2.Zero, lineFunc);
//                an.InsertKeyFrame(0.1f, new Vector2(0, -6f), lineFunc);
//                an.InsertKeyFrame(0.2f, Vector2.Zero, lineFunc);
//                an.InsertKeyFrame(1f, Vector2.Zero, lineFunc);
//                an.Duration = TimeSpan.FromSeconds(1.5);
//                an.IterationBehavior = AnimationIterationBehavior.Forever;
//                an.DelayTime = delayTime;

//                animation = an;

//                compositionShape!.StartAnimation("Offset", an);
//            }

//            public void Dispose()
//            {

//            }
//        }

//        public LyricClipByCharaterTextElement(Compositor compositor, IReadOnlyList<LyricTextGlyphRun> lyricTextGlyphRuns, Size layoutSize)
//        {
//            var effect = new GaussianBlurEffect()
//            {
//                Source = new CompositionEffectSourceParameter("source"),
//                BlurAmount = 20
//            };

//            var brush2 = compositor.CreateEffectFactory(effect).CreateBrush();
//            brush2.SetSourceParameter("source", compositor.CreateBackdropBrush());

//            var brush = compositor.CreateBackdropBrush();

//            dataList = new List<Data>();

//            var clipGeometry = CanvasGeometry.CreateRectangle(null, new Rect(-0.1, -0.1, layoutSize.Width + 0.1, layoutSize.Height + 0.1));

//            for (int i = 0; i < lyricTextGlyphRuns.Count; i++)
//            {
//                var glyphRun = lyricTextGlyphRuns[i];

//                var geo = CanvasGeometry.CreateGlyphRun(
//                    null,
//                    glyphRun.Point,
//                    glyphRun.FontFace,
//                    glyphRun.FontSize,
//                    glyphRun.Glyphs,
//                    glyphRun.IsSideways,
//                    glyphRun.BidiLevel,
//                    Microsoft.Graphics.Canvas.Text.CanvasTextMeasuringMode.Natural,
//                    glyphRun.GlyphOrientation);

//                var geometry = clipGeometry.CombineWith(geo, Matrix3x2.Identity, CanvasGeometryCombine.Intersect);
//                geo.Dispose();

//                dataList.Add(new Data(compositor, geometry, brush));

//                geometry.Dispose();
//            }

//            shapeVisual = compositor.CreateShapeVisual();
//            shapeVisual.Size = new Vector2((float)layoutSize.Width + 20, (float)layoutSize.Height + 20);

//            containerVisual = compositor.CreateContainerVisual();
//            containerVisual.Children.InsertAtTop(shapeVisual);
//            //containerVisual.Size = shapeVisual.Size;

//            var delayTime = TimeSpan.Zero;

//            foreach (var data in dataList)
//            {
//                shapeVisual.Shapes.Add(data.Shape);
//                data.StartAnimation(delayTime);
//                delayTime += TimeSpan.FromSeconds(0.1);
//            }
//            clipGeometry.Dispose();
//        }

//        public override Visual? Visual => containerVisual;
//    }

//    public class LyricClassicTextElement : LyricTextElement
//    {
//        private CompositionPath? compositionPath;
//        private CompositionGeometry? compositionGeometry;
//        private CompositionSpriteShape? compositionShape;
//        private ShapeVisual? shapeVisual;
//        private ContainerVisual? containerVisual;
//        private SpriteVisual? shadowVisual;
//        private LayerVisual? shadowHostVisual;

//        public LyricClassicTextElement(Compositor compositor, IReadOnlyList<LyricTextGlyphRun> lyricTextGlyphRuns, Size layoutSize)
//        {
//            CanvasGeometry? geometry = null;
//            var clipGeometry = CanvasGeometry.CreateRectangle(null, new Rect(-0.1, -0.1, layoutSize.Width + 0.1, layoutSize.Height + 0.1));

//            for (int i = 0; i < lyricTextGlyphRuns.Count; i++)
//            {
//                var glyphRun = lyricTextGlyphRuns[i];

//                var geo = CanvasGeometry.CreateGlyphRun(
//                    null,
//                    glyphRun.Point,
//                    glyphRun.FontFace,
//                    glyphRun.FontSize,
//                    glyphRun.Glyphs,
//                    glyphRun.IsSideways,
//                    glyphRun.BidiLevel,
//                    Microsoft.Graphics.Canvas.Text.CanvasTextMeasuringMode.Natural,
//                    glyphRun.GlyphOrientation);


//                if (geometry != null)
//                {
//                    geometry = geometry.CombineWith(geo, Matrix3x2.Identity, CanvasGeometryCombine.Union);
//                    geo.Dispose();
//                }
//                else
//                {
//                    geometry = geo;
//                }
//            }

//            if (geometry != null)
//            {
//                geometry = geometry.CombineWith(clipGeometry, Matrix3x2.Identity, CanvasGeometryCombine.Intersect);

//                compositionPath = new CompositionPath(geometry);

//                compositionGeometry = compositor.CreatePathGeometry(compositionPath);

//                compositionShape = compositor.CreateSpriteShape(compositionGeometry);

//                compositionShape.StrokeThickness = 0.2f;
//                compositionShape.FillBrush = compositor.CreateColorBrush(Colors.Black);
//                //compositionShape.StrokeBrush = compositor.CreateColorBrush(Colors.Black);

//                shapeVisual = compositor.CreateShapeVisual();
//                shapeVisual.Shapes.Add(compositionShape);
//                shapeVisual.Size = new Vector2((float)layoutSize.Width, (float)layoutSize.Height);

//                shadowVisual = compositor.CreateSpriteVisual();
//                shadowVisual.Size = shapeVisual.Size;
//                shadowVisual.Clip = compositor.CreateGeometricClip(compositionGeometry);
//                shadowVisual.Brush = compositor.CreateColorBrush(Color.FromArgb(128, 128, 128, 128));
//                shadowVisual.Offset = new Vector3(12, 12, 0);

//                shadowHostVisual = compositor.CreateLayerVisual();
//                shadowHostVisual.Size = shapeVisual.Size + new Vector2(24, 24);
//                shadowHostVisual.Children.InsertAtTop(shadowVisual);
//                shadowHostVisual.Effect = compositor.CreateEffectFactory(new GaussianBlurEffect()
//                {
//                    Source = new CompositionEffectSourceParameter("source"),
//                    BlurAmount = 4
//                }).CreateBrush();
//                shadowHostVisual.Offset = new Vector3(-12, -12, 0);

//                containerVisual = compositor.CreateContainerVisual();
//                containerVisual.Children.InsertAtBottom(shadowHostVisual);
//                containerVisual.Children.InsertAtTop(shapeVisual);
//                containerVisual.Size = shapeVisual.Size;
//            }

//            geometry?.Dispose();
//            clipGeometry.Dispose();
//        }

//        public override Visual? Visual => containerVisual;
//    }
//}
