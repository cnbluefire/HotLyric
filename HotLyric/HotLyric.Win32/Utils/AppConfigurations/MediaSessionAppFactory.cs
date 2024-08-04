using HotLyric.Win32.Models.AppConfigurationModels;
using HotLyric.Win32.Utils.MediaSessions.SMTC;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Media.Control;

namespace HotLyric.Win32.Utils.AppConfigurations
{
    public class MediaSessionAppFactory
    {
        private readonly AppConfigurationManager appConfigurationManager;
        private readonly Version ZeroVersion = new Version(0, 0, 0, 0);

        public MediaSessionAppFactory(AppConfigurationManager appConfigurationManager)
        {
            this.appConfigurationManager = appConfigurationManager;
        }

        public async Task<IReadOnlyList<AppConfigurationModel.MediaSessionAppModel>> GetAllAppsAsync(CancellationToken cancellationToken = default)
        {
            var config = await appConfigurationManager.GetLocalConfigurationAsync(cancellationToken);
            return config.AppConfiguration.MediaSessionApps;
        }

        public async Task<IReadOnlyList<(AppConfigurationModel.MediaSessionAppModel appModel, IReadOnlyList<GlobalSystemMediaTransportControlsSession> sessions)>> GetAppSessionPairs(
            IReadOnlyList<GlobalSystemMediaTransportControlsSession> sessions,
            CancellationToken cancellationToken = default)
        {
            var apps = await GetAllAppsAsync(cancellationToken);

            var list = new List<(AppConfigurationModel.MediaSessionAppModel, IReadOnlyList<GlobalSystemMediaTransportControlsSession>)>();
            var group = sessions.GroupBy(c => c.SourceAppUserModelId);

            foreach (var item in group)
            {
                foreach (var app in apps)
                {
                    if (app.Match.Regex.IsMatch(item.Key))
                    {
                        if (app.Match.MinSupportedVersion == ZeroVersion)
                        {
                            list.Add((app, item.ToArray()));
                        }
                        else if (app.SessionType == MediaSessionType.SMTC_PackagedApp)
                        {
                            try
                            {
                                var package = await ApplicationHelper.TryGetPackageFromAppUserModelIdAsync(item.Key, cancellationToken);
                                if (package != null)
                                {
                                    var packageVersion = package.Id.Version;
                                    var version = new Version(packageVersion.Major, packageVersion.Minor, packageVersion.Build, packageVersion.Revision);
                                    if (version >= app.Match.MinSupportedVersion)
                                    {
                                        list.Add((app, item.ToArray()));
                                    }
                                }
                            }
                            catch { }
                        }
                    }
                }
            }

            return list;
        }

        public async Task<SMTCManager> CreateSMTCManagerAsync(CancellationToken cancellationToken = default)
        {
            return await SMTCManager.CreateAsync(this, cancellationToken);
        }

    }
}
