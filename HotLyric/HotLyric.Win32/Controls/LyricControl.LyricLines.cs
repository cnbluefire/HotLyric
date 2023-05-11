using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HotLyric.Win32.Utils.LyricFiles;
using Microsoft.UI.Xaml.Documents;
using Windows.UI.StartScreen;

namespace HotLyric.Win32.Controls
{
    partial class LyricControl
    {
        private class LyricLines
        {
            private TimeSpan position;
            private Lyric? lyric;
            private bool skipEmptyLine = true;
            private bool isTranslateEnabled;

            internal LyricLines()
            {
                DrawingLocker = new object();
            }

            public object DrawingLocker { get; }

            public Lyric? Lyric
            {
                get => lyric;
                set
                {
                    if (lyric != value)
                    {
                        lyric = null;
                        UpdateLines();

                        lyric = value;
                        UpdateLines();
                    }
                }
            }

            public ILyricLine? MainLine { get; private set; }

            public ILyricLine? SecondaryLine { get; private set; }

            public ILyricLine? RemovingLine { get; private set; }

            public bool SecondaryLineIsTranslate { get; private set; }

            public bool SkipEmptyLine
            {
                get => skipEmptyLine;
                set
                {
                    if (skipEmptyLine != value)
                    {
                        lock (DrawingLocker)
                        {
                            skipEmptyLine = value;
                        }
                    }
                }
            }

            public bool IsTranslateEnabled
            {
                get => isTranslateEnabled;
                set
                {
                    lock (DrawingLocker)
                    {
                        if (isTranslateEnabled != value)
                        {
                            isTranslateEnabled = value;

                            MainLine = null;
                            SecondaryLine = null;
                            UpdateLines();
                        }
                    }
                }
            }

            public bool Paused
            {
                get
                {
                    lock (DrawingLocker)
                    {
                        return MainLine == null && SecondaryLine == null && RemovingLine == null;
                    }
                }
            }

            public TimeSpan Position
            {
                get => position;
                set
                {
                    if (Math.Abs((position - value).TotalSeconds) > 0.01)
                    {
                        position = value;
                        UpdateLines();
                    }
                }
            }

            public bool IsEmpty => lyric?.IsEmpty ?? false;

            private void UpdateLines()
            {
                lock (DrawingLocker)
                {
                    if (lyric == null)
                    {
                        MainLine = null;
                        SecondaryLine = null;
                        RemovingLine = null;
                        SecondaryLineIsTranslate = false;
                    }
                    else
                    {
                        if (lyric.IsEmpty)
                        {
                            if (!string.IsNullOrEmpty(lyric.SongName))
                            {
                                MainLine = new SongInfoLyricLine(lyric.SongName);
                                SecondaryLine = new SongInfoLyricLine(lyric.Artists ?? "");
                                SecondaryLineIsTranslate = false;
                                RemovingLine = null;
                            }
                            else
                            {
                                MainLine = lyric.Content.GetCurrentOrNextLine(TimeSpan.Zero, true);
                                SecondaryLine = null;
                                SecondaryLineIsTranslate = false;
                                RemovingLine = null;
                            }
                        }
                        else
                        {
                            var mainLine = MainLine;
                            var secondaryLine = SecondaryLine;
                            var removingLine = RemovingLine;
                            var secondaryLineIsTranslate = SecondaryLineIsTranslate;
                            var skipEmptyLine = SkipEmptyLine;

                            var position = Position;

                            bool updateMainLineFlag = false;

                            if (mainLine != null)
                            {
                                if (position < mainLine.StartTime || position >= mainLine.EndTime)
                                {
                                    updateMainLineFlag = true;
                                }
                            }
                            else
                            {
                                updateMainLineFlag = true;
                            }

                            if (updateMainLineFlag)
                            {
                                mainLine = lyric.Content.GetCurrentLine(position, skipEmptyLine);

                                if (mainLine != null)
                                {
                                    (secondaryLine, secondaryLineIsTranslate) = GetNextLine(lyric, mainLine.StartTime, skipEmptyLine);

                                    if (secondaryLineIsTranslate)
                                    {
                                        removingLine = null;
                                    }
                                    else
                                    {
                                        removingLine = lyric.Content.GetPreviousLine(mainLine.StartTime, skipEmptyLine);
                                    }
                                }
                                else
                                {
                                    secondaryLine = null;
                                    removingLine = null;
                                    secondaryLineIsTranslate = false;
                                }

                            }

                            MainLine = mainLine;
                            SecondaryLine = secondaryLine;
                            RemovingLine = removingLine;
                            SecondaryLineIsTranslate = secondaryLineIsTranslate;
                        }
                    }
                }

            }

            private (ILyricLine? line, bool isTranslate) GetNextLine(Lyric lyric, TimeSpan currentLineStartTime, bool skipEmpty)
            {
                if (lyric.Translate != null && IsTranslateEnabled)
                {
                    var translate = lyric.Translate.GetCurrentLine(currentLineStartTime, false);
                    if (translate != null
                        && currentLineStartTime >= translate.StartTime
                        && currentLineStartTime < (translate.StartTime + TimeSpan.FromSeconds(0.05)))
                    {
                        return (translate, true);
                    }
                }

                var line = lyric.Content.GetNextLine(currentLineStartTime, skipEmpty);
                if (line != null) return (line, false);

                return default;
            }

            private class SongInfoLyricLine : ILyricLine
            {
                public SongInfoLyricLine(string text)
                {
                    StartTime = TimeSpan.Zero;
                    EndTime = TimeSpan.Zero;
                    IsEndLine = false;
                    Text = text;
                    AllSpans = new[] { new SongInfoLyricSpan(text) };
                }

                public TimeSpan StartTime { get; }

                public TimeSpan EndTime { get; }

                public bool IsEndLine { get; }

                public string Text { get; }

                public IReadOnlyList<ILyricLineSpan> AllSpans { get; }

                public ILyricLineSpan? GetCurrentOrNextSpan(TimeSpan time)
                {
                    return AllSpans[0];
                }

                public ILyricLineSpan? GetCurrentSpan(TimeSpan time)
                {
                    return AllSpans[0];
                }

                public ILyricLineSpan? GetNextSpan(TimeSpan time)
                {
                    return AllSpans[0];
                }
            }

            private class SongInfoLyricSpan : ILyricLineSpan
            {
                public SongInfoLyricSpan(string text)
                {
                    StartTime = TimeSpan.Zero;
                    EndTime = TimeSpan.Zero;
                    Text = text;
                    IsEndSpan = true;
                    CharacterIndex = 0;
                    CharacterLength = text.Length;
                }

                public TimeSpan StartTime { get; }

                public TimeSpan EndTime { get; }

                public string Text { get; }

                public bool IsEndSpan { get; }

                public int CharacterIndex { get; }

                public int CharacterLength { get; }
            }
        }
    }
}
