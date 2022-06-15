using HotLyric.Win32.Utils.MediaSessions.SMTC;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace HotLyric.Win32.Utils.MediaSessions
{
    public class MediaSessionManagerFactory
    {
        private bool? isSupported;

        public async Task<SMTCManager?> GetManagerAsync(CancellationToken cancellationToken = default)
        {
            return await GetManagerAsync(SMTCApps.AllApps.Values.ToArray(), cancellationToken).ConfigureAwait(false);
        }

        public async Task<SMTCManager?> GetManagerAsync(IReadOnlyList<SMTCApp>? supportedApps, CancellationToken cancellationToken = default)
        {
            supportedApps ??= SMTCApps.AllApps.Values.ToArray();

            if (isSupported == false)
            {
                return null;
            }

            try
            {
                var manager = await SMTCManager.CreateAsync(supportedApps, cancellationToken).ConfigureAwait(false);
                isSupported = true;
                return manager;
            }
            catch (Exception ex) when (!(ex is OperationCanceledException))
            {
                if (!isSupported.HasValue) isSupported = false;
            }
            return null;
        }
    }
}
