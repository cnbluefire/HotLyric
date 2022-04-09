using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.UI.Composition;
using System;
using System.Collections.Generic;
using System.Text;
using Windows.Foundation;
using Windows.UI.Composition;

namespace HotLyric.Win32.BackgroundHelpers
{
    public class CompositionSurfaceFactory : IDisposable
    {
        private CompositionDrawingSurface? surface;
        private CompositionGraphicsDevice? graphicsDevice;
        private bool firstAccessSurface = true;
        private bool disposedValue;

        public CompositionSurfaceFactory(Windows.Foundation.Size size)
        {
            graphicsDevice = DeviceHolder.GraphicsDevice;
            graphicsDevice.RenderingDeviceReplaced += GraphicsDevice_RenderingDeviceReplaced;

            surface = graphicsDevice.CreateDrawingSurface(
                size,
                Windows.Graphics.DirectX.DirectXPixelFormat.B8G8R8A8UIntNormalized,
                Windows.Graphics.DirectX.DirectXAlphaMode.Premultiplied);
        }

        private void GraphicsDevice_RenderingDeviceReplaced(CompositionGraphicsDevice sender, RenderingDeviceReplacedEventArgs args)
        {
            OnDraw();
        }

        public void InvalidateSurface()
        {
            OnDraw();
        }

        public void Resize(Windows.Foundation.Size size)
        {
            CanvasComposition.Resize(surface, size);
            InvalidateSurface();
        }

        public CompositionDrawingSurface Surface
        {
            get
            {
                if (firstAccessSurface)
                {
                    InvalidateSurface();
                }
                return surface!;
            }
        }

        private void OnDraw()
        {
            firstAccessSurface = false;

            if (Draw == null) return;

            try
            {
                using (var ds = CanvasComposition.CreateDrawingSession(surface))
                {
                    var args = new CompositionSurfaceFactoryDrawEventArgs(Surface, ds);
                    Draw.Invoke(this, args);
                }
            }
            catch (Exception ex) when (DeviceHolder.CanvasDevice.IsDeviceLost(ex.HResult))
            {
                DeviceHolder.CanvasDevice.RaiseDeviceLost();
            }
        }

        public event TypedEventHandler<CompositionSurfaceFactory, CompositionSurfaceFactoryDrawEventArgs>? Draw;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: 释放托管状态(托管对象)
                    if (graphicsDevice != null)
                    {
                        graphicsDevice.RenderingDeviceReplaced -= GraphicsDevice_RenderingDeviceReplaced;
                        graphicsDevice = null;
                    }

                    surface?.Dispose();
                    surface = null;
                }

                // TODO: 释放未托管的资源(未托管的对象)并重写终结器
                // TODO: 将大型字段设置为 null
                disposedValue = true;
            }
        }

        // // TODO: 仅当“Dispose(bool disposing)”拥有用于释放未托管资源的代码时才替代终结器
        // ~CompositionSurfaceFactory()
        // {
        //     // 不要更改此代码。请将清理代码放入“Dispose(bool disposing)”方法中
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            // 不要更改此代码。请将清理代码放入“Dispose(bool disposing)”方法中
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }

    public class CompositionSurfaceFactoryDrawEventArgs
    {
        public CompositionSurfaceFactoryDrawEventArgs(CompositionDrawingSurface surface, CanvasDrawingSession drawingSession)
        {
            Surface = surface;
            DrawingSession = drawingSession;
        }

        public CompositionDrawingSurface Surface { get; }

        public CanvasDrawingSession DrawingSession { get; }
    }
}
