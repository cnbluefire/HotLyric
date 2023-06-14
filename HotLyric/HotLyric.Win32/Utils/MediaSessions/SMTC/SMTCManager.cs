using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Media.Control;

namespace HotLyric.Win32.Utils.MediaSessions.SMTC
{
    public class SMTCManager : IDisposable
    {
        private bool disposedValue;

        private GlobalSystemMediaTransportControlsSessionManager manager;
        private readonly IReadOnlyList<SMTCApp> supportedApps;
        private ISMTCSession[]? sessions;
        private ISMTCSession? curSession;
        private Task initSessionTask;

        private SMTCManager(GlobalSystemMediaTransportControlsSessionManager manager, IReadOnlyList<SMTCApp> supportedApps)
        {
            this.manager = manager ?? throw new ArgumentNullException(nameof(manager));
            this.supportedApps = supportedApps;

            manager.SessionsChanged += Manager_SessionsChanged;
            initSessionTask = UpdateSessionsAsync();
        }

        private async void Manager_SessionsChanged(GlobalSystemMediaTransportControlsSessionManager sender, SessionsChangedEventArgs args)
        {
            await UpdateSessionsAsync();
            SessionsChanged?.Invoke(this, EventArgs.Empty);
        }

        private async Task UpdateSessionsAsync()
        {
            var list = new List<ISMTCSession>();
            var appIdHash = new HashSet<string>();

            var tmp = manager.GetSessions().ToArray();
            var tmp2 = new List<(SMTCApp, List<GlobalSystemMediaTransportControlsSession>)>();

            foreach (var session in tmp)
            {
                var sessionApp = await GetAppAsync(session);
                if (sessionApp != null)
                {
                    if (sessionApp.PackageFamilyNamePrefix == "903DB504.12708F202F598_"
                        || sessionApp.PackageFamilyNamePrefix == "903DB504.QQWP_")
                    {
                        // QQ音乐创造性的使用两个GSMTC来控制音乐
                        // 一个用来提供媒体信息，一个用来提供时间轴信息
                        var list2 = tmp2.FirstOrDefault(c => c.Item1.PackageFamilyNamePrefix == sessionApp.PackageFamilyNamePrefix).Item2;
                        if (list2 != null)
                        {
                            list2.Add(session);
                        }
                        else
                        {
                            tmp2.Add((sessionApp, new List<GlobalSystemMediaTransportControlsSession>() { session }));
                        }
                    }
                    else
                    {
                        tmp2.Add((sessionApp, new List<GlobalSystemMediaTransportControlsSession>() { session }));
                    }
                }
            }

            var curSession = manager.GetCurrentSession();
            var curApp = tmp2.FirstOrDefault(c => c.Item2?.Contains(curSession) == true);

            if (curApp.Item1 != null)
            {
                var s = CreateMediaSession(curApp.Item2, curApp.Item1, sessions);

                list.Add(s);
                appIdHash.Add(curSession.SourceAppUserModelId);
                this.curSession = s;
            }
            else
            {
                this.curSession = null;
            }

            foreach (var (app2, sessionGroup) in tmp2)
            {
                if (app2 != null && appIdHash.Add(sessionGroup[0].SourceAppUserModelId))
                {
                    var s = CreateMediaSession(sessionGroup, app2, sessions);
                    list.Add(s);
                }
            }

            sessions = list.ToArray();

            var removed = sessions.Where(c => !list.Contains(c)).ToList();
            foreach (var item in removed)
            {
                item.Dispose();
            }
        }

        private ISMTCSession CreateMediaSession(IReadOnlyList<GlobalSystemMediaTransportControlsSession> sessionGroup, SMTCApp app, IReadOnlyList<ISMTCSession>? oldSessions)
        {
            var result = oldSessions?.FirstOrDefault(c => sessionGroup.Contains(c.Session));
            if (result != null) return result;

            if (app.AppId == "com.electron.yesplaymusic" || app.AppId == "YesPlayMusic.exe")
            {
                return new YesPlayerMusicSession(sessionGroup[0], app);
            }
            return new SMTCSession(sessionGroup, app);
        }

        private async Task<SMTCApp?> GetAppAsync(GlobalSystemMediaTransportControlsSession session)
        {
            if (session == null) return null;

            string appid = "";
            try
            {
                appid = session.SourceAppUserModelId;
            }
            catch (Exception ex)
            {
                HotLyric.Win32.Utils.LogHelper.LogError(ex);
                return null;
            }

            foreach (var item in supportedApps)
            {
                if (appid.StartsWith(item.PackageFamilyNamePrefix, StringComparison.OrdinalIgnoreCase))
                {
                    if (item.MinSupportedVersion != null)
                    {
                        var package = await ApplicationHelper.TryGetPackageFromAppUserModelIdAsync(appid);
                        if (package != null)
                        {
                            try
                            {
                                var v = package.Id.Version;
                                var packageVersion = new Version(v.Major, v.Minor, v.Build, v.Revision);
                                if (packageVersion >= item.MinSupportedVersion) return item;
                            }
                            catch (Exception ex)
                            {
                                HotLyric.Win32.Utils.LogHelper.LogError(ex);
                            }
                        }
                    }
                    else
                    {
                        return item;
                    }
                }
            }

            return null;
        }

        public ISMTCSession? CurrentSession => curSession;

        public IReadOnlyList<ISMTCSession> Sessions => sessions?.ToArray() ?? Array.Empty<ISMTCSession>();


        public event EventHandler? SessionsChanged;

        internal event EventHandler? Disposing;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    Disposing?.Invoke(this, EventArgs.Empty);

                    manager.SessionsChanged -= Manager_SessionsChanged;
                    manager = null!;
                    sessions = null;
                    // TODO: 释放托管状态(托管对象)
                }

                // TODO: 释放未托管的资源(未托管的对象)并重写终结器
                // TODO: 将大型字段设置为 null
                disposedValue = true;
            }
        }

        // // TODO: 仅当“Dispose(bool disposing)”拥有用于释放未托管资源的代码时才替代终结器
        // ~SMTCManager()
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

        public static async Task<SMTCManager> CreateAsync(IReadOnlyList<SMTCApp> supportedApps, CancellationToken cancellationToken = default)
        {
            if (supportedApps is null || supportedApps.Count == 0)
            {
                throw new ArgumentNullException(nameof(supportedApps));
            }

            var manager = await GlobalSystemMediaTransportControlsSessionManager.RequestAsync().AsTask(cancellationToken);

            var smtcManager = new SMTCManager(manager, supportedApps);
            await smtcManager.initSessionTask.ConfigureAwait(false);

            return smtcManager;
        }
    }
}
