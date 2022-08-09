using System;
using System.Collections.Generic;
using System.Text;
using Vanara.PInvoke;

namespace HotLyric.Win32.Utils
{
    public static class DeviceHelper
    {
        private static object locker = new object();
        private static bool? isLowPerformanceDevice;

        public static bool IsLowPerformanceDevice
        {
            get
            {
                if (!isLowPerformanceDevice.HasValue)
                {
                    lock (locker)
                    {
                        if (!isLowPerformanceDevice.HasValue)
                        {
                            isLowPerformanceDevice = CheckLowPerformanceDevice();
                        }
                    }
                }

                return isLowPerformanceDevice.Value;
            }
        }

        public static bool HasTouchDeviceOrPen
        {
            get
            {
                var flag = GetDigitizerFlag();

                return flag != SM_DIGITIZER_FLAG.TABLET_CONFIG_NONE
                    && (flag & SM_DIGITIZER_FLAG.NID_READY) != 0
                    && ((flag & SM_DIGITIZER_FLAG.NID_INTEGRATED_TOUCH) != 0
                        || (flag & SM_DIGITIZER_FLAG.NID_EXTERNAL_TOUCH) != 0
                        || (flag & SM_DIGITIZER_FLAG.NID_INTEGRATED_PEN) != 0
                        || (flag & SM_DIGITIZER_FLAG.NID_EXTERNAL_PEN) != 0);

            }
        }

        private static SM_DIGITIZER_FLAG GetDigitizerFlag()
        {
            return (SM_DIGITIZER_FLAG)User32.GetSystemMetrics(User32.SystemMetric.SM_DIGITIZER);
        }

        private static bool CheckLowPerformanceDevice()
        {
            var processorCount = Environment.ProcessorCount;
            if (processorCount <= 4)
            {
                // 逻辑核心（包括超线程）小于等于4时认为是低性能设备
                return true;
            }

            var memoryInfo = Kernel32.MEMORYSTATUSEX.Default;
            if (Kernel32.GlobalMemoryStatusEx(ref memoryInfo))
            {
                var memoryMB = memoryInfo.ullTotalPhys / 1024 / 1024;

                if (memoryMB <= 3.3 * 1024)
                {
                    // 内存小于3.3G认为是低性能设备
                    return true;
                }
                else if (memoryMB >= 10 * 1024)
                {
                    // 内存大于10G认为不是低性能设备
                    return false;
                }
            }

            if (HasTouchDeviceOrPen && User32.GetSystemMetrics(User32.SystemMetric.SM_CONVERTIBLESLATEMODE) == 0)
            {
                // 认为平板是低性能设备
                return true;
            }

            return false;
        }

        [Flags]
        private enum SM_DIGITIZER_FLAG
        {
            TABLET_CONFIG_NONE = 0x00000000, //    The input digitizer does not have touch capabilities.
            NID_INTEGRATED_TOUCH = 0x00000001, // An integrated touch digitizer is used for input.
            NID_EXTERNAL_TOUCH = 0x00000002, //   An external touch digitizer is used for input.
            NID_INTEGRATED_PEN = 0x00000004, //   An integrated pen digitizer is used for input.
            NID_EXTERNAL_PEN = 0x00000008, // An external pen digitizer is used for input.
            NID_MULTI_INPUT = 0x00000040, //  An input digitizer with support for multiple inputs is used for input.
            NID_READY = 0x00000080, //The input digitizer is ready for input. If this value is unset, it may mean that the tablet service is stopped, the digitizer is not supported, or digitizer drivers have not been installed.
        }

    }
}
