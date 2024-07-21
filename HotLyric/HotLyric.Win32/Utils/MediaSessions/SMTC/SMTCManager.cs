using HotLyric.Win32.Models.AppConfigurationModels;
using HotLyric.Win32.Utils.AppConfigurations;
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
        private readonly MediaSessionAppFactory mediaSessionAppFactory;
        private GlobalSystemMediaTransportControlsSessionManager manager;
        private ISMTCSession[]? sessions;
        private ISMTCSession? curSession;

        private SMTCManager(MediaSessionAppFactory mediaSessionAppFactory, GlobalSystemMediaTransportControlsSessionManager manager)
        {
            this.mediaSessionAppFactory = mediaSessionAppFactory;
            this.manager = manager ?? throw new ArgumentNullException(nameof(manager));

            manager.SessionsChanged += Manager_SessionsChanged;
        }

        private async void Manager_SessionsChanged(GlobalSystemMediaTransportControlsSessionManager sender, SessionsChangedEventArgs args)
        {
            await UpdateSessionsAsync();
            SessionsChanged?.Invoke(this, EventArgs.Empty);
        }

        private async Task UpdateSessionsAsync(CancellationToken cancellationToken = default)
        {
            var list = new List<ISMTCSession>();
            var appIdHash = new HashSet<string>();

            var tmp = manager.GetSessions().ToArray();

            var pairs = await mediaSessionAppFactory.GetAppSessionPairs(tmp, cancellationToken);

            var curSession = manager.GetCurrentSession();
            var curApp = pairs.FirstOrDefault(c => c.sessions.Contains(curSession));

            if (curApp.appModel != null)
            {
                var s = CreateMediaSession(curApp.sessions, curApp.appModel, sessions);

                list.Add(s);
                appIdHash.Add(curSession.SourceAppUserModelId);
                this.curSession = s;
            }
            else
            {
                this.curSession = null;
            }

            foreach (var (app2, sessionGroup) in pairs)
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

        private ISMTCSession CreateMediaSession(IReadOnlyList<GlobalSystemMediaTransportControlsSession> sessionGroup, AppConfigurationModel.MediaSessionAppModel app, IReadOnlyList<ISMTCSession>? oldSessions)
        {
            var result = oldSessions?.FirstOrDefault(c => sessionGroup.Contains(c.Session));
            if (result != null) return result;

            if (app.SessionType == MediaSessionType.YesPlayMusic)
            {
                return new YesPlayerMusicSession(sessionGroup[0], app);
            }
            return new SMTCSession(sessionGroup, app);
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

        public static async Task<SMTCManager> CreateAsync(MediaSessionAppFactory mediaSessionAppFactory, CancellationToken cancellationToken = default)
        {
            var manager = await GlobalSystemMediaTransportControlsSessionManager.RequestAsync().AsTask(cancellationToken);

            var smtcManager = new SMTCManager(mediaSessionAppFactory, manager);
            await smtcManager.UpdateSessionsAsync(cancellationToken);

            return smtcManager;
        }
    }
}
