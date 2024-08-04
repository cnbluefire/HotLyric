using BlueFire.Toolkit.WinUI3.Extensions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HotLyric.Win32.Models;
using HotLyric.Win32.Utils.AppConfigurations;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;

namespace HotLyric.Win32.ViewModels
{
    public partial class AppConfigurationSettingsViewModel : ObservableObject
    {
        private readonly AppConfigurationManager configManager;

        public AppConfigurationSettingsViewModel(AppConfigurationManager configManager)
        {
            this.configManager = configManager;
            this.sources = new ObservableCollection<AppConfigurationSourceModel>();
        }

        private bool isConfigurationUpdating;
        private bool isConfigurationSourceChanging;
        private ObservableCollection<AppConfigurationSourceModel> sources;
        private AsyncRelayCommand? saveSourcesCommand;
        private AsyncRelayCommand? updateConfigurationCommand;
        private DateTimeOffset? lastUpdateTime;
        private Task? loadAppConfigurationTask;

        public bool IsConfigurationUpdating
        {
            get => isConfigurationUpdating;
            private set
            {
                if (SetProperty(ref isConfigurationUpdating, value))
                {
                    UpdateConfigurationCommand.NotifyCanExecuteChanged();
                }
            }
        }

        public bool IsConfigurationSourceChanging
        {
            get => isConfigurationSourceChanging;
            private set => SetProperty(ref isConfigurationSourceChanging, value);
        }

        public ObservableCollection<AppConfigurationSourceModel> Sources
        {
            get
            {
                if (loadAppConfigurationTask == null)
                {
                    loadAppConfigurationTask = LoadAppConfigurationAsync();
                }
                return sources;
            }
            private set => SetProperty(ref sources, value);
        }

        public string LastUpdateTimeDisplayText
        {
            get
            {
                if (lastUpdateTime.HasValue)
                {
                    return lastUpdateTime.Value.ToString("g");
                }
                else
                {
                    return string.Empty;
                }
            }
        }

        public AsyncRelayCommand UpdateConfigurationCommand => updateConfigurationCommand ??= new AsyncRelayCommand(async () => await UpdateConfigurationAsync(true), () => true);

        private async Task LoadAppConfigurationAsync()
        {
            IsConfigurationSourceChanging = true;
            IsConfigurationUpdating = true;
            try
            {
                await ReloadSourcesAsync();
                await UpdateLastUpdateTimeAsync();
            }
            finally
            {
                IsConfigurationSourceChanging = false;
                IsConfigurationUpdating = false;
            }
        }

        private async Task UpdateLastUpdateTimeAsync()
        {
            var localConfiguration = await configManager.GetLocalConfigurationAsync();
            if (!string.IsNullOrEmpty(localConfiguration.Source))
            {
                lastUpdateTime = DateTimeOffset.FromUnixTimeSeconds(localConfiguration.UpdateTime).ToLocalTime();
            }
            else
            {
                lastUpdateTime = null;
            }
            OnPropertyChanged(nameof(LastUpdateTimeDisplayText));
        }


        public async Task UpdateConfigurationAsync(bool showResultDialog = false)
        {
            if (IsConfigurationUpdating) return;
            try
            {
                IsConfigurationUpdating = true;

                var result = await configManager.UpdateConfigurationAsync();
                if (result)
                {
                    await UpdateLastUpdateTimeAsync();

                }

                if (showResultDialog && App.Current.SettingsView?.AppWindow?.IsVisible is true)
                {
                    var tb = new TextBlock()
                    {
                        TextWrapping = Microsoft.UI.Xaml.TextWrapping.Wrap,
                        MaxWidth = 350,
                        HorizontalAlignment = Microsoft.UI.Xaml.HorizontalAlignment.Left,
                    };

                    if (result)
                    {
                        var updateResultModel = await configManager.GetLocalConfigurationAsync();

                        var hyperlink = new Microsoft.UI.Xaml.Documents.Hyperlink()
                        {
                            Inlines =
                            {
                                new Microsoft.UI.Xaml.Documents.Run()
                                {
                                    Text = updateResultModel.Source
                                }
                            }
                        };
                        hyperlink.Click += async (s, a) =>
                        {
                            await new AppConfigurationSourceModel()
                            {
                                Uri = updateResultModel.Source
                            }.LaunchUriCommand.ExecuteAsync(null);
                        };
                        tb.Inlines.Add(new Microsoft.UI.Xaml.Documents.Run() { Text = "使用 \"" });
                        tb.Inlines.Add(hyperlink);
                        tb.Inlines.Add(new Microsoft.UI.Xaml.Documents.Run() { Text = "\" 更新成功。" });
                    }
                    else
                    {
                        tb.Text = "更新失败，请检查源配置或网络配置后重试。";
                    }

                    var dialog = new ContentDialog()
                    {
                        PrimaryButtonText = "确定",
                        DefaultButton = ContentDialogButton.Primary,
                        Background = null,
                        Title = "更新 App 配置",
                        Content = tb,
                    };

                    await dialog.ShowModalWindowAsync();
                }
            }
            finally
            {
                IsConfigurationUpdating = false;
            }
        }

        [RelayCommand]
        private async Task DeleteSourceAsync(AppConfigurationSourceModel model)
        {
            try
            {
                IsConfigurationSourceChanging = true;

                if (sources.Count <= 1) return;

                var dialog = new ContentDialog()
                {
                    PrimaryButtonText = "确定",
                    CloseButtonText = "取消",
                    DefaultButton = ContentDialogButton.Close,
                    Background = null,
                    Content = new TextBlock()
                    {
                        Text = $"确定要删除\"{model.Uri}\"这个更新源吗？",
                        TextWrapping = Microsoft.UI.Xaml.TextWrapping.WrapWholeWords
                    }
                };
                var result = await dialog.ShowModalWindowAsync();
                if (result == ContentDialogResult.Primary)
                {
                    sources.Remove(model);
                    await SaveSourcesAsync();
                    await ReloadSourcesAsync();
                }
            }
            finally
            {
                IsConfigurationSourceChanging = false;
            }

        }

        [RelayCommand]
        private async Task UpdateSourceEnableStateAsync(AppConfigurationSourceModel model)
        {
            try
            {
                IsConfigurationSourceChanging = true;

                if (!model.Enabled)
                {
                    if (sources.All(c => !c.Enabled))
                    {
                        model.Enabled = true;
                        return;
                    }
                }

                await SaveSourcesAsync();
                await ReloadSourcesAsync();
            }
            finally
            {
                IsConfigurationSourceChanging = false;
            }
        }

        [RelayCommand]
        private async Task ResetSourcesAsync()
        {
            try
            {
                IsConfigurationSourceChanging = true;

                await configManager.ResetSourcesAsync();
                await ReloadSourcesAsync();
            }
            finally
            {
                IsConfigurationSourceChanging = false;
            }
        }

        [RelayCommand]
        private async Task AddSourceAsync()
        {
            try
            {
                IsConfigurationSourceChanging = true;

                var tb = new TextBox()
                {
                    IsSpellCheckEnabled = false,
                    PlaceholderText = "请输入配置文件的 url",
                    TextWrapping = Microsoft.UI.Xaml.TextWrapping.Wrap,
                    AcceptsReturn = true,
                    HorizontalAlignment = Microsoft.UI.Xaml.HorizontalAlignment.Stretch,
                    VerticalAlignment = Microsoft.UI.Xaml.VerticalAlignment.Stretch,
                    Width = 300,
                    Height = 200
                };

                var dialog = new ContentDialog()
                {
                    PrimaryButtonText = "确定",
                    CloseButtonText = "取消",
                    DefaultButton = ContentDialogButton.Primary,
                    Background = null,
                    Title = "添加源",
                    Content = tb,
                };
                var result = await dialog.ShowModalWindowAsync();
                if (result == ContentDialogResult.Primary)
                {
                    var model = new AppConfigurationSourceModel()
                    {
                        Uri = tb.Text.Trim(),
                        Enabled = true,
                    };
                    if (model.RequestUri != null)
                    {
                        bool flag = false;
                        for (int i = 0; i < sources.Count; i++)
                        {
                            if (string.Equals(sources[i].Uri, model.Uri, StringComparison.OrdinalIgnoreCase))
                            {
                                sources[i].Enabled = true;
                                flag = true;
                                break;
                            }
                        }

                        if (!flag)
                        {
                            sources.Add(model);
                        }
                        await SaveSourcesAsync();
                        await ReloadSourcesAsync();
                    }
                }
            }
            finally
            {
                IsConfigurationSourceChanging = false;
            }
        }

        private async Task ReloadSourcesAsync()
        {
            var result = await configManager.GetSourcesAsync();

            Sources = new ObservableCollection<AppConfigurationSourceModel>(result.Select(c => new AppConfigurationSourceModel()
            {
                Enabled = c.Enabled,
                Uri = c.Uri,
            }));
        }

        private async Task SaveSourcesAsync()
        {
            AppConfigurationManager.AppConfigurationSourceModel[]? result = null;
            try
            {
                var list = sources.ToList();
                result = await configManager.SetSourcesAsync(list.Select(c => new AppConfigurationManager.AppConfigurationSourceModel()
                {
                    Enabled = c.Enabled,
                    Uri = c.Uri,
                }).ToArray());
            }
            catch { }

            if (result == null)
            {
                result = await configManager.GetSourcesAsync();
            }

            Sources = new ObservableCollection<AppConfigurationSourceModel>(result.Select(c => new AppConfigurationSourceModel()
            {
                Enabled = c.Enabled,
                Uri = c.Uri,
            }));
        }

    }
}
