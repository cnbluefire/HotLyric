using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace HotLyric.Win32.Utils.SystemMediaTransportControls
{
    public class SMTCFactory
    {
        private static readonly IReadOnlyList<string> DefaultSessionsPrefix = SMTCApps.AllApps.Values.Select(c => c.PackageFamilyNamePrefix).ToArray();

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

        private class ListEqualityComparer : IEqualityComparer<IReadOnlyList<string>>
        {
            public bool Equals(IReadOnlyList<string>? x, IReadOnlyList<string>? y)
            {
                if (x == null && y == null) return true;
                else if (x == null || y == null) return false;
                else return Enumerable.SequenceEqual(x, y, StringComparer.OrdinalIgnoreCase);
            }

            public int GetHashCode([DisallowNull] IReadOnlyList<string> obj)
            {
                var code = 0;
                foreach (var item in obj)
                {
                    code = HashCode.Combine(item);
                }

                return code;
            }
        }
    }
}
