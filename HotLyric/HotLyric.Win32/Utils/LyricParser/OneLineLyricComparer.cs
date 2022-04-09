#nullable disable

using System.Collections.Generic;

namespace Kfstorm.LrcParser
{
    internal class OneLineLyricComparer : IComparer<IOneLineLyric>
    {
        public int Compare(IOneLineLyric x, IOneLineLyric y)
        {
            return x.Timestamp.CompareTo(y.Timestamp);
        }
    }
}
