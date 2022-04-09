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
        private readonly IReadOnlyList<string> sessionsPrefix;
        private SMTCSession[]? sessions;
        private SMTCSession? curSession;


        private SMTCManager(GlobalSystemMediaTransportControlsSessionManager manager, IReadOnlyList<string> sessionsPrefix)
        {
            this.manager = manager ?? throw new ArgumentNullException(nameof(manager));
            this.sessionsPrefix = sessionsPrefix;

            manager.SessionsChanged += Manager_SessionsChanged;
            UpdateSessions();
        }

        private void Manager_SessionsChanged(GlobalSystemMediaTransportControlsSessionManager sender, SessionsChangedEventArgs args)
        {
            UpdateSessions();
            SessionsChanged?.Invoke(this, EventArgs.Empty);
        }

        private void UpdateSessions()
        {
            var list = new List<SMTCSession>();
            var appIdHash = new HashSet<string>();

            var tmp = manager.GetSessions();
            var curSession = manager.GetCurrentSession();

            if (IsValidSession(curSession))
            {
                var s = new SMTCSession(curSession);
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
                if (IsValidSession(session) && appIdHash.Add(session.SourceAppUserModelId))
                {
                    var s = new SMTCSession(session);
                    list.Add(s);
                }
            }

            sessions = list.ToArray();
        }

        private bool IsValidSession(GlobalSystemMediaTransportControlsSession session)
        {
            if (session == null) return false;

            string appid = "";
            try
            {
                appid = session.SourceAppUserModelId;
            }
            catch
            {
                return false;
            }

            foreach (var item in sessionsPrefix)
            {
                if (appid.StartsWith(item, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
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

        public static async Task<SMTCManager> CreateAsync(IReadOnlyList<string> sessionsPrefix, CancellationToken cancellationToken = default)
        {
            var manager = await GlobalSystemMediaTransportControlsSessionManager.RequestAsync().AsTask(cancellationToken);

            return new SMTCManager(manager, sessionsPrefix);
        }
    }
}
