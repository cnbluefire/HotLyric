﻿using HotLyric.Win32.Utils.AppConfigurations;
using HotLyric.Win32.Utils.MediaSessions;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HotLyric.Win32.ViewModels
{
    public class ViewModelLocator : IServiceProvider
    {
        private IServiceProvider serviceProvider;

        public ViewModelLocator()
        {
            var services = new ServiceCollection();
            ConfigureServices(services);
            serviceProvider = services.BuildServiceProvider();
        }

        private void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<AppConfigurationManager>();
            services.AddSingleton<MediaSessionAppFactory>();
            services.AddSingleton<LyricWindowViewModel>();
            services.AddSingleton<SettingsWindowViewModel>();
            services.AddSingleton<AppConfigurationSettingsViewModel>();
        }

        public LyricWindowViewModel LyricWindowViewModel => this.GetRequiredService<LyricWindowViewModel>();


        public SettingsWindowViewModel SettingsWindowViewModel => this.GetRequiredService<SettingsWindowViewModel>();


        public AppConfigurationSettingsViewModel AppConfigurationSettingsViewModel => this.GetRequiredService<AppConfigurationSettingsViewModel>();

        public static ViewModelLocator Instance => (ViewModelLocator)App.Current.Resources["Locator"];

        public object? GetService(Type serviceType)
        {
            return serviceProvider.GetService(serviceType);
        }
    }
}
