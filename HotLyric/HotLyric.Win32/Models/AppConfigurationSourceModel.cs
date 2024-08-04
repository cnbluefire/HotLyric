using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Threading.Tasks;
using Windows.System;

namespace HotLyric.Win32.Models
{
    public partial class AppConfigurationSourceModel : ObservableObject
    {
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(RequestUri))]
        private string? uri;

        [ObservableProperty]
        private bool enabled;

        public Uri? RequestUri
        {
            get
            {
                if (global::System.Uri.TryCreate(Uri, UriKind.Absolute, out var result))
                {
                    return result;
                }
                return null;
            }
        }

        [RelayCommand]
        public async Task LaunchUriAsync()
        {
            if (global::System.Uri.TryCreate(Uri, UriKind.Absolute, out var result))
            {
                try
                {
                    await Launcher.LaunchUriAsync(result, new LauncherOptions()
                    {
                        TreatAsUntrusted = false
                    });
                }
                catch { }
            }
        }
    }
}
