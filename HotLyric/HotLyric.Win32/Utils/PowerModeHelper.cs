using H.NotifyIcon.EfficiencyMode;
using Microsoft.Win32;
using Microsoft.Windows.System.Power;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using Windows.Devices.Power;

namespace HotLyric.Win32.Utils
{
    public class PowerModeHelper : IDisposable
    {
        private static Lazy<bool> isSupportEfficiencyMode = new Lazy<bool>(() =>
        {
            return Environment.OSVersion.Platform == PlatformID.Win32NT &&
                Environment.OSVersion.Version >= new Version(6, 2);
        }, true);

        public static bool IsSupportEfficiencyMode => isSupportEfficiencyMode.Value;

        private bool disposeValue;
        private Battery battery;
        private BatteryReport? batteryReport;
        private Timer updateTimer;

        public PowerModeHelper()
        {
            battery = Battery.AggregateBattery;

            battery.ReportUpdated += Battery_ReportUpdated;
            PowerManager.EffectivePowerModeChanged += PowerManager_EffectivePowerModeChanged;

            updateTimer = new Timer(TimeSpan.FromSeconds(0.5));
            updateTimer.Elapsed += UpdateTimer_Elapsed;

            UpdateProperties();
        }

        public double RemainingBatteryPercentage { get; private set; }

        public EffectivePowerMode EffectivePowerMode { get; private set; }

        public BatteryStatus BatteryStatus { get; private set; }

        private void Battery_ReportUpdated(Battery sender, object args)
        {
            UpdateProperties();
            updateTimer.Stop();
            updateTimer.Start();
        }

        private void PowerManager_EffectivePowerModeChanged(object? sender, object e)
        {
            UpdateProperties();
            updateTimer.Stop();
            updateTimer.Start();
        }

        private void UpdateProperties()
        {
            EffectivePowerMode = PowerManager.EffectivePowerMode2;

            batteryReport = battery.GetReport();

            BatteryStatus = batteryReport.Status switch
            {
                Windows.System.Power.BatteryStatus.NotPresent => BatteryStatus.NotPresent,
                Windows.System.Power.BatteryStatus.Discharging => BatteryStatus.Discharging,
                Windows.System.Power.BatteryStatus.Idle => BatteryStatus.Idle,
                Windows.System.Power.BatteryStatus.Charging => BatteryStatus.Charging,
                _ => throw new ArgumentException(nameof(BatteryStatus))
            };

            var total = batteryReport.FullChargeCapacityInMilliwattHours ??
                batteryReport.DesignCapacityInMilliwattHours;

            var remaining = batteryReport.RemainingCapacityInMilliwattHours;

            if (total.HasValue && remaining.HasValue)
            {
                RemainingBatteryPercentage = (remaining * 1.0 / total).Value;
            }
            else
            {
                RemainingBatteryPercentage = 1;
            }
        }

        private void UpdateTimer_Elapsed(object? sender, ElapsedEventArgs e)
        {
            ((Timer?)sender)?.Stop();
            OnPropertiesChanged();
        }

        public event EventHandler? PropertiesChanged;

        private void OnPropertiesChanged()
        {
            PropertiesChanged?.Invoke(this, EventArgs.Empty);
        }

        public void Dispose()
        {
            if (!disposeValue)
            {
                disposeValue = true;

                updateTimer.Elapsed -= UpdateTimer_Elapsed;
                battery.ReportUpdated -= Battery_ReportUpdated;

                updateTimer.Stop();
                updateTimer.Dispose();
                updateTimer = null!;

                battery = null!;
            }
        }



        public static void SetEfficiencyMode(bool enable)
        {
            if (enable)
            {
                if (IsSupportEfficiencyMode)
                {
                    EfficiencyModeUtilities.SetEfficiencyMode(true);
                }
            }
            else
            {
                EfficiencyModeUtilities.SetEfficiencyMode(false);
            }
        }


    }
}
