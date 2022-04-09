using Kfstorm.LrcParser;
using System;
using System.Collections.Generic;
using System.Text;

namespace HotLyric.Win32.Utils
{
    public class LrcFileWrapper
    {
        private readonly ILrcFile lrcFile;
        private bool skipEmptyLine;
        private TimeSpan position;
        private TimeSpan mediaDuration;

        public LrcFileWrapper(ILrcFile lrcFile, TimeSpan position, bool skipEmptyLine)
        {
            this.lrcFile = lrcFile;
            this.position = position;
            this.skipEmptyLine = skipEmptyLine;

            UpdateLines();
        }

        public LrcFileWrapper(ILrcFile lrcFile) : this(lrcFile, TimeSpan.Zero, false) { }

        public ILrcFile LrcFile => lrcFile;

        public bool SkipEmptyLine
        {
            get => skipEmptyLine;
            set
            {
                if (skipEmptyLine != value)
                {
                    skipEmptyLine = value;
                    UpdateLines();
                }
            }
        }

        public TimeSpan Position
        {
            get => position;
            set
            {
                if (position != value)
                {
                    position = value;
                    UpdateLines();
                }
            }
        }

        public TimeSpan MediaDuration
        {
            get => mediaDuration;
            set
            {
                if (mediaDuration != value)
                {
                    mediaDuration = value;
                    UpdateLines();
                }
            }
        }

        public IOneLineLyric? CurrentLine { get; private set; }

        public IOneLineLyric? ActualCurrentLine { get; private set; }

        public IOneLineLyric? NextLine { get; private set; }

        public TimeSpan CurrentLineDuration { get; private set; }

        public TimeSpan ActualCurrentLineDuration { get; private set; }

        public TimeSpan NextLineDuration { get; private set; }

        public bool ActualNextLine => ActualCurrentLine != null && CurrentLine != ActualCurrentLine;

        public bool IsFinalLine => NextLine == null;

        private void UpdateLines()
        {
            var line = GetCurrentLine(Position, SkipEmptyLine, out var actualLine);
            var nextLine = GetNextLine(line, SkipEmptyLine);

            CurrentLine = line;
            ActualCurrentLine = actualLine;
            NextLine = nextLine;

            CurrentLineDuration = GetLineDuration(CurrentLine);
            ActualCurrentLineDuration = GetLineDuration(ActualCurrentLine);
            NextLineDuration = GetLineDuration(NextLine);
        }

        public IOneLineLyric? GetCurrentLine(TimeSpan position, bool skipEmptyLine, out IOneLineLyric? actuanLine)
        {
            actuanLine = null;

            if (lrcFile == null) return null;

            IOneLineLyric? line = null;

            do
            {
                if (line == null)
                {
                    line = lrcFile.BeforeOrAt(position);
                    actuanLine = line;

                    if (skipEmptyLine 
                        && line == null 
                        && lrcFile.Lyrics.Count > 0 
                        && lrcFile.Lyrics[0].Timestamp > position)
                    {
                        line = lrcFile.Lyrics[0];
                    }
                }
                else
                {
                    line = LrcFile?.After(line.Timestamp);
                }

            } while (skipEmptyLine && line != null && string.IsNullOrWhiteSpace(line.Content));

            return line;
        }

        public IOneLineLyric? GetNextLine(IOneLineLyric? currentLine, bool skipEmptyLine)
        {
            if (lrcFile == null || currentLine == null) return null;

            var line = currentLine;

            if (line != null)
            {
                do
                {
                    line = lrcFile.After(line.Timestamp);

                } while (skipEmptyLine && line != null && string.IsNullOrWhiteSpace(line.Content));
            }

            return line;
        }

        private TimeSpan GetLineDuration(IOneLineLyric? line)
        {
            if (lrcFile == null || line == null) return TimeSpan.Zero;

            var nextLine = lrcFile.After(line.Timestamp);

            if (nextLine != null)
            {
                return nextLine.Timestamp - line.Timestamp;
            }
            else if (MediaDuration > line.Timestamp)
            {
                return MediaDuration - line.Timestamp;
            }

            return TimeSpan.Zero;
        }
    }
}
