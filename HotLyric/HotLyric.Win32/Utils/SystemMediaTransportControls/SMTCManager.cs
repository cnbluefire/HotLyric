using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Media.Control;

namespace HotLyric.Win32.Utils.SystemMediaTransportControls
{
    public class SMTCManager : IDisposable
    {
        private bool disposedValue;

        private GlobalSystemMediaTransportControlsSessionManager manager;
        private readonly IReadOnlyList<SMTCApp> supportedApps;
        private SMTCSession[]? sessions;
        private SMTCSession? curSession;


        private SMTCManager(GlobalSystemMediaTransportControlsSessionManager manager, IReadOnlyList<SMTCApp> supportedApps)
        {
            this.manager = manager ?? throw new ArgumentNullException(nameof(manager));
            this.supportedApps = supportedApps;

            manager.SessionsChanged += Manager_SessionsChanged;
            _ = UpdateSessionsAsync();
        }

        private async void Manager_SessionsChanged(GlobalSystemMediaTransportControlsSessionManager sender, SessionsChangedEventArgs args)
        {
            await UpdateSessionsAsync();
            SessionsChanged?.Invoke(this, EventArgs.Empty);
        }

        private async Task UpdateSessionsAsync()
        {
            var list = new List<SMTCSession>();
            var appIdHash = new HashSet<string>();

            var tmp = manager.GetSessions();
            var curSession = manager.GetCurrentSession();

            var app = await GetAppAsync(curSession);
            if (app != null)
            {
                var s = new SMTCSession(curSession, app.PositionMode, app);
                list.Add(s);
                appIdHash.Add(curSession.SourceAppUserModelId);
                this.curSession = s;
            }
            else
            {
                this.curSession = null;
            }

            foreach (var session in tmp)
            {
                var app2 = await GetAppAsync(session);
                if (app2 != null && appIdHash.Add(session.SourceAppUserModelId))
                {
                    var s = new SMTCSession(session, app2.PositionMode, app2);
                    list.Add(s);
                }
            }

            sessions = list.ToArray();
        }

        private async Task<SMTCApp?> GetAppAsync(GlobalSystemMediaTransportControlsSession session)
        {
            if (session == null) return null;

            string appid = "";
            try
            {
                appid = session.SourceAppUserModelId;
            }
            catch
            {
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
                            catch { }
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

        public SMTCSession? CurrentSession => curSession;

        public IReadOnlyList<SMTCSession> Sessions => sessions?.ToArray() ?? Array.Empty<SMTCSession>();


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

            return new SMTCManager(manager, supportedApps);
        }
    }
}
