using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.UI.Composition;
using System;
using System.Collections.Generic;
using System.Text;
using Windows.UI.Composition;

namespace HotLyric.Win32.BackgroundHelpers
{
    public static class DeviceHolder
    {
        private static CanvasDevice? canvasDevice;
        private static CompositionGraphicsDevice? graphicsDevice;


        public static CanvasDevice CanvasDevice
        {
            get
            {
                if (canvasDevice == null)
                {
                    lock (typeof(DeviceHolder))
                    {
                        if (canvasDevice == null)
                        {
                            canvasDevice = CanvasDevice.GetSharedDevice();
                            canvasDevice.DeviceLost += CanvasDevice_DeviceLost;
                        }
                    }
                }
                return canvasDevice;
            }
        }

        public static CompositionGraphicsDevice GraphicsDevice
        {
            get
            {
                if (graphicsDevice == null)
                {
                    lock (typeof(DeviceHolder))
                    {
                        if (graphicsDevice == null)
                        {
                            graphicsDevice = CanvasComposition.CreateCompositionGraphicsDevice(CompositionThread.Instance.Compositor, CanvasDevice);
                        }
                    }
                }

                return graphicsDevice;
            }
        }

        private static void CanvasDevice_DeviceLost(CanvasDevice sender, object args)
        {
            sender.DeviceLost -= CanvasDevice_DeviceLost;
            canvasDevice = CanvasDevice.GetSharedDevice();
            if (graphicsDevice != null)
            {
                CanvasComposition.SetCanvasDevice(graphicsDevice, canvasDevice);
            }
            canvasDevice.DeviceLost += CanvasDevice_DeviceLost;
        }
    }
}
