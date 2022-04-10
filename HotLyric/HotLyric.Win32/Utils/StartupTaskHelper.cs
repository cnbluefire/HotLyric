using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.UI.ViewManagement;

namespace HotLyric.Win32.Utils
{
    public class StartupTaskHelper : ObservableObject
    {
        private bool isStartupTaskEnabled;
        private bool isStartupTaskCanEnable;
        private AsyncRelayCommand? enableStartupTaskCommand;
        private AsyncRelayCommand? disableStartupTaskCommand;
        private AsyncRelayCommand? toggleStartupTaskCommand;

        public StartupTaskHelper(string startupTaskId)
        {
            if (string.IsNullOrEmpty(startupTaskId))
            {
                throw new ArgumentException(nameof(startupTaskId));
            }

            StartupTaskId = startupTaskId;
            _ = RefreshAsync();
        }

        public string StartupTaskId { get; }

        /// <summary>
        /// 已开启开机启动
        /// </summary>
        public bool IsStartupTaskEnabled
        {
            get => isStartupTaskEnabled;
            set => SetProperty(ref isStartupTaskEnabled, value);
        }

        /// <summary>
        /// 可以启用开机启动
        /// </summary>
        public bool IsStartupTaskCanEnable
        {
            get => isStartupTaskCanEnable;
            set => SetProperty(ref isStartupTaskCanEnable, value);
        }

        public AsyncRelayCommand EnableStartupTaskCommand => enableStartupTaskCommand ?? (enableStartupTaskCommand = new AsyncRelayCommand(EnableAsync));

        public AsyncRelayCommand DisableStartupTaskCommand => disableStartupTaskCommand ?? (disableStartupTaskCommand = new AsyncRelayCommand(DisableAsync));

        public AsyncRelayCommand ToggleStartupTaskCommand => toggleStartupTaskCommand ?? (toggleStartupTaskCommand = new AsyncRelayCommand(ToggleAsync));

        public async Task EnableAsync()
        {
            try
            {
                var startupTask = await StartupTask.GetAsync(StartupTaskId);
                var state = await startupTask.RequestEnableAsync();
                RefreshCore(state);
            }
            catch
            {
                RefreshCore(StartupTaskState.DisabledByPolicy);
            }
        }

        public async Task DisableAsync()
        {
            try
            {
                var startupTask = await StartupTask.GetAsync(StartupTaskId);
                startupTask.Disable();
                startupTask = await StartupTask.GetAsync(StartupTaskId);
                RefreshCore(startupTask.State);
            }
            catch
            {
                RefreshCore(StartupTaskState.DisabledByPolicy);
            }
        }

        public async Task ToggleAsync()
        {
            var startupTask = await StartupTask.GetAsync(StartupTaskId);
            if (startupTask.State == StartupTaskState.Enabled || startupTask.State == StartupTaskState.EnabledByPolicy)
            {
                await DisableAsync();
            }
            else
            {
                await EnableAsync();
            }
        }

        public async Task RefreshAsync()
        {
            try
            {
                var startupTask = await StartupTask.GetAsync(StartupTaskId);
                RefreshCore(startupTask.State);
            }
            catch
            {
                RefreshCore(StartupTaskState.DisabledByPolicy);
            }
        }

        private void RefreshCore(StartupTaskState state)
        {
            isStartupTaskCanEnable = state switch
            {
                StartupTaskState.Enabled => true,
                StartupTaskState.EnabledByPolicy => false,
                StartupTaskState.Disabled => true,
                StartupTaskState.DisabledByUser => false,
                StartupTaskState.DisabledByPolicy => false,
                _ => false
            };

            isStartupTaskEnabled = state switch
            {
                StartupTaskState.Enabled => true,
                StartupTaskState.EnabledByPolicy => true,
                StartupTaskState.Disabled => false,
                StartupTaskState.DisabledByUser => false,
                StartupTaskState.DisabledByPolicy => false,
                _ => false
            };

            OnPropertyChanged(nameof(IsStartupTaskCanEnable));
            OnPropertyChanged(nameof(IsStartupTaskEnabled));
        }
    }
}
