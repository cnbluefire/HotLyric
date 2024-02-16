using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HotLyric.Win32.Utils.LyricFiles
{
    public class ClassicLyricDocument : ILyricDocument
    {
        private ClassicLyricDocument(IEnumerable<ClassicLyricLine> lines)
        {
            AllLines = lines.ToArray();
        }

        public IReadOnlyList<ILyricLine> AllLines { get; }

        public ILyricLine? GetCurrentLine(TimeSpan time, bool skipEmpty)
        {
            if (AllLines.Count > 0 && time > AllLines[^1].EndTime)
            {
                return AllLines[^1];
            }

            for (int i = 0; i < AllLines.Count; i++)
            {
                var line = AllLines[i];
                if (line.EndTime > time)
                {
                    if (skipEmpty)
                    {
                        if (!string.IsNullOrEmpty(line.Text))
                        {
                            return line;
                        }
                    }
                    else if (line.StartTime <= time)
                    {
                        return line;
                    }
                }
            }
            return null;
        }

        public ILyricLine? GetNextLine(TimeSpan time, bool skipEmpty)
        {
            if (AllLines.Count > 0 && time > AllLines[^1].EndTime)
            {
                return null;
            }

            ILyricLine? curLine = null;

            for (int i = 0; i < AllLines.Count; i++)
            {
                var line = AllLines[i];

                if (line.EndTime > time)
                {
                    if (curLine == null)
                    {
                        if (skipEmpty && !string.IsNullOrEmpty(line.Text))
                        {
                            curLine = line;
                        }
                        else if (line.StartTime <= time)
                        {
                            curLine = line;
                        }
                    }
                    else
                    {
                        if (skipEmpty)
                        {
                            if (!string.IsNullOrEmpty(line.Text))
                            {
                                return line;
                            }
                        }
                        else
                        {
                            return line;
                        }
                    }
                }
            }
            return null;
        }

        public ILyricLine? GetPreviousLine(TimeSpan time, bool skipEmpty)
        {
            for (int i = AllLines.Count - 1; i >= 0; i--)
            {
                var line = AllLines[i];
                if (line.EndTime <= time)
                {
                    if (skipEmpty)
                    {
                        if (!string.IsNullOrEmpty(line.Text))
                        {
                            return line;
                        }
                    }
                    else
                    {
                        return line;
                    }
                }
            }
            return null;
        }

        public ILyricLine? GetCurrentOrNextLine(TimeSpan time, bool skipEmpty)
        {
            return GetCurrentLine(time, skipEmpty) ?? GetNextLine(time, skipEmpty);
        }

        public static ILyricDocument? Create(string lyricContent)
        {
            if (string.IsNullOrEmpty(lyricContent)) return null;

            var lrcFile = Lyricify.Lyrics.Helpers.ParseHelper.ParseLyrics(lyricContent, Lyricify.Lyrics.Models.LyricsRawTypes.Lrc);
            if (lrcFile?.Lines != null)
            {
                var list = new List<ClassicLyricLine>();

                for (int i = 0; i < lrcFile.Lines.Count; i++)
                {
                    var line = lrcFile.Lines[i];
                    var nextLine = i + 1 < lrcFile.Lines.Count ? lrcFile.Lines[i + 1] : null;

                    bool isEnd = nextLine == null;

                    var span = new ClassicLyricLineSpan(
                        TimeSpan.FromMilliseconds(line.StartTime ?? 0),
                        nextLine?.StartTime is not null ? TimeSpan.FromMilliseconds((double)nextLine?.StartTime!) : TimeSpan.FromDays(100),
                        line.Text,
                        0,
                        line.Text.Length,
                        isEnd);

                    list.Add(new ClassicLyricLine(span));
                }

                return new ClassicLyricDocument(list);
            }

            return null;
        }

        private class ClassicLyricLine : ILyricLine
        {
            public ClassicLyricLine(ClassicLyricLineSpan span)
            {
                AllSpans = new[] { span };

                StartTime = span.StartTime;
                EndTime = span.EndTime;

                IsEndLine = span.IsEndSpan;
            }

            public TimeSpan StartTime { get; }

            public TimeSpan EndTime { get; }

            public bool IsEndLine { get; }

            public IReadOnlyList<ILyricLineSpan> AllSpans { get; }

            public string Text => AllSpans[0].Text;

            public ILyricLineSpan? GetCurrentSpan(TimeSpan time)
            {
                return AllSpans.FirstOrDefault(c => c.StartTime <= time && c.EndTime > time);
            }

            public ILyricLineSpan? GetNextSpan(TimeSpan time)
            {
                return AllSpans.FirstOrDefault(c => c.StartTime > time);
            }

            public ILyricLineSpan? GetCurrentOrNextSpan(TimeSpan time)
            {
                return GetCurrentSpan(time) ?? GetNextSpan(time);
            }
        }

        private class ClassicLyricLineSpan : ILyricLineSpan
        {
            public ClassicLyricLineSpan(TimeSpan startTime, TimeSpan endTime, string text, int charIndex, int charLength, bool isEndSpan)
            {
                StartTime = startTime;
                EndTime = endTime;
                Text = text;
                CharIndex = charIndex;
                CharLength = charLength;
                IsEndSpan = isEndSpan;
            }

            public TimeSpan StartTime { get; }

            public TimeSpan EndTime { get; }

            public string Text { get; }

            public int CharIndex { get; }

            public int CharLength { get; }

            public int CharacterIndex { get; }

            public int CharacterLength { get; }

            public bool IsEndSpan { get; }
        }
    }
}
