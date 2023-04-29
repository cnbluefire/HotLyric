using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HotLyric.Win32.Utils.LyricFiles
{
    public interface ILyricDocument
    {
        IReadOnlyList<ILyricLine> AllLines { get; }

        ILyricLine? GetCurrentLine(TimeSpan time, bool skipEmpty);

        ILyricLine? GetNextLine(TimeSpan time, bool skipEmpty);

        ILyricLine? GetCurrentOrNextLine(TimeSpan time, bool skipEmpty);

        ILyricLine? GetPreviousLine(TimeSpan time, bool skipEmpty);

    }

    public interface ILyricLine
    {
        TimeSpan StartTime { get; }

        TimeSpan EndTime { get; }

        bool IsEndLine { get; }

        string Text { get; }

        IReadOnlyList<ILyricLineSpan> AllSpans { get; }

        ILyricLineSpan? GetCurrentSpan(TimeSpan time);

        ILyricLineSpan? GetNextSpan(TimeSpan time);

        ILyricLineSpan? GetCurrentOrNextSpan(TimeSpan time);

    }

    public interface ILyricLineSpan
    {
        TimeSpan StartTime { get; }

        TimeSpan EndTime { get; }

        string Text { get; }

        bool IsEndSpan { get; }

        int CharacterIndex { get; }

        int CharacterLength { get; }
    }
}
