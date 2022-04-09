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

        private static ConcurrentDictionary<IReadOnlyList<string>, SMTCManager> cache =
            new ConcurrentDictionary<IReadOnlyList<string>, SMTCManager>(new ListEqualityComparer());

        public async Task<SMTCManager?> GetManagerAsync(CancellationToken cancellationToken = default)
        {
            return await GetManagerAsync(DefaultSessionsPrefix, cancellationToken).ConfigureAwait(false);
        }

        public async Task<SMTCManager?> GetManagerAsync(IReadOnlyList<string>? sessionsPrefix, CancellationToken cancellationToken = default)
        {
            sessionsPrefix ??= DefaultSessionsPrefix;

            if (isSupported == true)
            {
                var manager = GetCachedManager(sessionsPrefix);
                if (manager != null) return manager;
            }
            else if (isSupported == false)
            {
                return null;
            }

            try
            {
                var manager = await SMTCManager.CreateAsync(sessionsPrefix, cancellationToken).ConfigureAwait(false);
                isSupported = true;
                return manager;
            }
            catch (Exception ex) when (!(ex is OperationCanceledException))
            {
                if (!isSupported.HasValue) isSupported = false;
            }
            return null;
        }

        private SMTCManager? GetCachedManager(IReadOnlyList<string>? sessionsPrefix)
        {
            sessionsPrefix = sessionsPrefix ?? DefaultSessionsPrefix;

            lock (cache)
            {
                var key = sessionsPrefix.OrderBy(c => c).ToArray();
                if (cache.TryGetValue(key, out var value))
                {
                    return value;
                }
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
