using HotLyric.Win32.Utils;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.UI.Composition;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Graphics.DirectX.Direct3D11;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;
using Windows.UI.Composition;
using LoadedImageSourceLoadStatus = Microsoft.UI.Xaml.Media.LoadedImageSourceLoadStatus;
using WinRT;

namespace HotLyric.Win32.Base.BackgroundHelpers
{
    public class CompositionSurfaceLoader : IDisposable
    {
        private bool disposeValue;

        private static D2DDeviceHolder? softwareD2dDeviceHolder;
        private static CompositionGraphicsDevice? graphicsDevice;

        private CompositionDrawingSurface? surface;
        private CancellationTokenSource cts;

        #region Static Create Methods

        public static CompositionSurfaceLoader StartLoadFromStream(IRandomAccessStream randomAccessStream, Size desiredMaxSize)
        {
            return new CompositionSurfaceLoader(randomAccessStream, desiredMaxSize);
        }

        public static CompositionSurfaceLoader StartLoadFromStream(IRandomAccessStream randomAccessStream)
        {
            return new CompositionSurfaceLoader(randomAccessStream, new Size(0, 0));
        }

        public static CompositionSurfaceLoader StartLoadFromUri(Uri uri, Size desiredMaxSize)
        {
            return new CompositionSurfaceLoader(uri, desiredMaxSize);
        }

        public static CompositionSurfaceLoader StartLoadFromUri(Uri uri)
        {
            return new CompositionSurfaceLoader(uri, new Size(0, 0));
        }

        #endregion Static Create Methods


        #region Constructors

        private CompositionSurfaceLoader()
        {
            CreateDevice();
            CreateSurface();
            cts = new CancellationTokenSource();
        }

        public CompositionSurfaceLoader(IRandomAccessStream randomAccessStream, Size desiredMaxSize) : this()
        {
            graphicsDevice!.DispatcherQueue.TryEnqueue(Windows.System.DispatcherQueuePriority.Low, async () =>
            {
                try
                {
                    await DrawImageCore(randomAccessStream, desiredMaxSize, cts.Token);
                    LoadCompleted?.Invoke(this, new SurfaceLoadCompletedEventArgs(LoadedImageSourceLoadStatus.Success, null));
                }
                catch (Exception ex)
                {
                    HotLyric.Win32.Utils.LogHelper.LogError(ex);
                    LoadCompleted?.Invoke(this, new SurfaceLoadCompletedEventArgs(ConvertExceptionToStatus(ex), ex));
                }
            });
        }

        public CompositionSurfaceLoader(Uri uri, Size desiredMaxSize) : this()
        {
            graphicsDevice!.DispatcherQueue.TryEnqueue(Windows.System.DispatcherQueuePriority.Low, async () =>
            {
                try
                {
                    using var stream = await UriResourceHelper.GetStreamAsync(uri, cts.Token);
                    await DrawImageCore(stream, desiredMaxSize, cts.Token);
                    LoadCompleted?.Invoke(this, new SurfaceLoadCompletedEventArgs(LoadedImageSourceLoadStatus.Success, null));
                }
                catch (Exception ex)
                {
                    HotLyric.Win32.Utils.LogHelper.LogError(ex);
                    LoadCompleted?.Invoke(this, new SurfaceLoadCompletedEventArgs(ConvertExceptionToStatus(ex), ex));
                }
            });
        }

        #endregion Constructors


        #region Create Resource

        private void CreateDevice()
        {
            if (softwareD2dDeviceHolder == null)
            {
                softwareD2dDeviceHolder = new D2DDeviceHolder(true);

                var compositor = WindowsCompositionHelper.Compositor;
                var interop = compositor.As<ICompositorInterop>();

                var raw = interop.CreateGraphicsDevice(softwareD2dDeviceHolder.D2D1Device);

                graphicsDevice = CompositionGraphicsDevice.FromAbi(raw);
            }
        }

        private void CreateSurface()
        {
            surface = graphicsDevice!.CreateDrawingSurface2(
                new(0, 0),
                Windows.Graphics.DirectX.DirectXPixelFormat.B8G8R8A8UIntNormalized,
                Windows.Graphics.DirectX.DirectXAlphaMode.Premultiplied);
        }

        #endregion Create Resource


        #region Draw Images

        private async Task DrawImageCore(IRandomAccessStream stream, Size desiredMaxSize, CancellationToken cancellationToken)
        {
            stream.Seek(0);

            var decoder = await BitmapDecoder.CreateAsync(stream)
                .AsTask(cancellationToken);

            var width = decoder.PixelWidth;
            var height = decoder.PixelHeight;

            var maxPixelWidth = desiredMaxSize.Width * decoder.DpiX / 96;
            var maxPixelHeight = desiredMaxSize.Height * decoder.DpiX / 96;

            var dWidth = 0d;
            var dHeight = 0d;

            if (maxPixelWidth == 0 && maxPixelHeight == 0)
            {
                dWidth = width;
                dHeight = height;
            }
            else if (maxPixelWidth != 0 && maxPixelHeight != 0)
            {
                dWidth = maxPixelWidth;
                dHeight = height / width * maxPixelWidth;

                if (dHeight > maxPixelHeight)
                {
                    dHeight = maxPixelHeight;
                    dWidth = width / height * maxPixelHeight;
                }
            }
            else if (maxPixelWidth != 0)
            {
                dWidth = maxPixelWidth;
                dHeight = height / width * maxPixelWidth;
            }
            else if (maxPixelHeight != 0)
            {
                dHeight = maxPixelHeight;
                dWidth = width / height * maxPixelHeight;
            }

            if (dWidth > maxPixelWidth || dHeight > maxPixelHeight)
            {
                dWidth = width;
                dHeight = height;
            }

            var dScaledWidth = dWidth * 96 / decoder.DpiX;
            var dScaledHeight = dHeight * 96 / decoder.DpiY;

            NaturalSize = new Size(width, height);
            DecodedPhysicalSize = new Size(dWidth, dHeight);
            DecodedSize = new Size(dScaledWidth, dScaledHeight);

            var transform = new BitmapTransform()
            {
                ScaledWidth = (uint)dWidth,
                ScaledHeight = (uint)dHeight
            };

            NaturalSize = new Size(width, height);

            var image = await decoder.GetSoftwareBitmapAsync(
                BitmapPixelFormat.Bgra8,
                BitmapAlphaMode.Premultiplied,
                transform,
                ExifOrientationMode.RespectExifOrientation,
                ColorManagementMode.DoNotColorManage).AsTask(cancellationToken);

            var interop = surface.As<DirectN.ICompositionDrawingSurfaceInterop>();

            interop.Resize(new DirectN.tagSIZE(image.PixelWidth, image.PixelHeight));

            DrawImage(interop, image, cancellationToken);

            unsafe static void DrawImage(DirectN.ICompositionDrawingSurfaceInterop _interop, SoftwareBitmap _softwareBitmap, CancellationToken _cancellationToken)
            {
                lock (graphicsDevice!)
                {
                    _interop.BeginDraw(0, typeof(DirectN.ID2D1DeviceContext).GUID, out var updateObject, out var updateOffset)
                        .ThrowOnError();
                    try
                    {
                        var deviceContext = updateObject.As<DirectN.ID2D1DeviceContext>();

                        var offsetX = updateOffset.x * 96 / (float)_softwareBitmap.DpiX;
                        var offsetY = updateOffset.y * 96 / (float)_softwareBitmap.DpiY;
                        var transform = DirectN.D2D_MATRIX_3X2_F.Translation(offsetX, offsetY);

                        deviceContext.SetTransform(ref transform);
                        deviceContext.SetDpi((float)_softwareBitmap.DpiX, (float)_softwareBitmap.DpiY);

                        _cancellationToken.ThrowIfCancellationRequested();

                        var bitmap = CreateBitmap(_softwareBitmap, deviceContext);

                        _cancellationToken.ThrowIfCancellationRequested();

                        deviceContext.DrawBitmap(bitmap, 0, 1, DirectN.D2D1_BITMAP_INTERPOLATION_MODE.D2D1_BITMAP_INTERPOLATION_MODE_LINEAR, 0);
                    }
                    finally
                    {
                        _interop.EndDraw().ThrowOnError();
                    }
                }
            }

            unsafe static DirectN.ID2D1Bitmap1 CreateBitmap(SoftwareBitmap _softwareBitmap, DirectN.ID2D1DeviceContext _deviceContext)
            {
                var _props = new DirectN.D2D1_BITMAP_PROPERTIES1()
                {
                    pixelFormat = new DirectN.D2D1_PIXEL_FORMAT()
                    {
                        alphaMode = DirectN.D2D1_ALPHA_MODE.D2D1_ALPHA_MODE_PREMULTIPLIED,
                        format = DirectN.DXGI_FORMAT.DXGI_FORMAT_B8G8R8A8_UNORM,
                    },
                    dpiX = (float)_softwareBitmap.DpiX,
                    dpiY = (float)_softwareBitmap.DpiY,
                };

                using var _buffer = _softwareBitmap.LockBuffer(BitmapBufferAccessMode.Read);

                var _reference = _buffer.CreateReference();
                var _bitmapDesc = _buffer.GetPlaneDescription(0);

                _reference.As<IMemoryBufferByteAccess>().GetBuffer(out byte* _pBuffer, out uint _bufferSize);

                _deviceContext.CreateBitmap(
                    new DirectN.D2D_SIZE_U((uint)_softwareBitmap.PixelWidth, (uint)_softwareBitmap.PixelHeight),
                    new nint(_pBuffer + _bitmapDesc.StartIndex),
                    (uint)_bitmapDesc.Stride,
                    ref _props,
                    out var _bitmap).ThrowOnError();

                return _bitmap;
            }
        }

        #endregion Draw Images


        public ICompositionSurface Surface => surface!;

        public Size DecodedPhysicalSize { get; private set; }

        public Size DecodedSize { get; private set; }

        public Size NaturalSize { get; private set; }

        public event SurfaceLoadCompletedEventHandler? LoadCompleted;

        public void Dispose()
        {
            if (!disposeValue)
            {
                disposeValue = true;

                cts?.Cancel();
                surface?.Dispose();
            }
        }

        #region Utilities

        private static LoadedImageSourceLoadStatus ConvertExceptionToStatus(Exception ex)
        {
            if (ex is HttpRequestException) return LoadedImageSourceLoadStatus.NetworkError;
            else if (ex.HResult == unchecked((int)0x88982f07)) return LoadedImageSourceLoadStatus.InvalidFormat;

            return LoadedImageSourceLoadStatus.Other;
        }

        #endregion Utilities


        #region D2DDeviceHolder

        private class D2DDeviceHolder
        {
            private DirectN.IComObject<DirectN.ID2D1Factory2> d2D1Factory2;
            private DirectN.IComObject<DirectN.ID3D11Device> d3d11Device;

            public D2DDeviceHolder(bool useSoftwareRenderer)
            {
                var options = new DirectN.D2D1_FACTORY_OPTIONS()
                {
                    debugLevel = DirectN.D2D1_DEBUG_LEVEL.D2D1_DEBUG_LEVEL_NONE,
                };

                d2D1Factory2 = DirectN.D2D1Functions.D2D1CreateFactory<DirectN.ID2D1Factory2>(
                    DirectN.D2D1_FACTORY_TYPE.D2D1_FACTORY_TYPE_MULTI_THREADED,
                    options);

                DirectN.D3D_DRIVER_TYPE driverType = useSoftwareRenderer ?
                    DirectN.D3D_DRIVER_TYPE.D3D_DRIVER_TYPE_WARP :
                    DirectN.D3D_DRIVER_TYPE.D3D_DRIVER_TYPE_HARDWARE;

                var flag = DirectN.D3D11_CREATE_DEVICE_FLAG.D3D11_CREATE_DEVICE_BGRA_SUPPORT;

                var featureLevels = new[]
                {
                    DirectN.D3D_FEATURE_LEVEL.D3D_FEATURE_LEVEL_11_1,
                    DirectN.D3D_FEATURE_LEVEL.D3D_FEATURE_LEVEL_11_0,
                    DirectN.D3D_FEATURE_LEVEL.D3D_FEATURE_LEVEL_10_1,
                    DirectN.D3D_FEATURE_LEVEL.D3D_FEATURE_LEVEL_10_0,
                    DirectN.D3D_FEATURE_LEVEL.D3D_FEATURE_LEVEL_9_3,
                    DirectN.D3D_FEATURE_LEVEL.D3D_FEATURE_LEVEL_9_2,
                    DirectN.D3D_FEATURE_LEVEL.D3D_FEATURE_LEVEL_9_1
                };

                d3d11Device = DirectN.D3D11Functions.D3D11CreateDevice(
                    null,
                    driverType,
                    flag,
                    featureLevels,
                    DirectN.D3D11Constants.D3D11_SDK_VERSION);

                DXGIDevice = d3d11Device.As<DirectN.IDXGIDevice3>();

                D2D1Factory2.CreateDevice(DXGIDevice, out DirectN.ID2D1Device1 d2d1Device).ThrowOnError();
                D2D1Device = d2d1Device;
            }

            public DirectN.ID2D1Factory2 D2D1Factory2 => d2D1Factory2.Object;

            public DirectN.ID3D11Device D3D11Device => d3d11Device.Object;

            public DirectN.IDXGIDevice3 DXGIDevice { get; }

            public DirectN.ID2D1Device1 D2D1Device { get; }
        }

        #endregion D2DDeviceHolder


        #region Interop

        [ComImport]
        [Guid("25297D5C-3AD4-4C9C-B5CF-E36A38512330")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface ICompositorInterop
        {
            public ICompositionSurface CreateCompositionSurfaceForHandle(IntPtr swapChain);

            public ICompositionSurface CreateCompositionSurfaceForSwapChain(IntPtr swapChain);

            public IntPtr CreateGraphicsDevice(DirectN.ID2D1Device1 renderingDevice);
        }

        [ComImport]
        [Guid("5B0D3235-4DBA-4D44-865E-8F1D0E4FD04D")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        unsafe interface IMemoryBufferByteAccess
        {
            void GetBuffer(out byte* buffer, out uint capacity);
        }

        #endregion Interop
    }

    public delegate void SurfaceLoadCompletedEventHandler(CompositionSurfaceLoader sender, SurfaceLoadCompletedEventArgs args);

    public record SurfaceLoadCompletedEventArgs(Microsoft.UI.Xaml.Media.LoadedImageSourceLoadStatus Status, Exception? Exception);
}
