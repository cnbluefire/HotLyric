using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace HotLyric.Win32.Utils.LrcProviders
{
    public interface ILrcProvider
    {
        string Name { get; }

        Task<LyricModel?> GetByIdAsync(object id, CancellationToken cancellationToken);

        Task<object?> GetIdAsync(string name, string[]? artists, CancellationToken cancellationToken);
    }
}
