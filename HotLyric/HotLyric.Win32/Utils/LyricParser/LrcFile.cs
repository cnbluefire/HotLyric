#nullable disable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Kfstorm.LrcParser
{
    /// <summary>
    /// Represents an LRC format lyrics file.
    /// </summary>
    /// <remarks>
    /// Duplicate timestamps is not supported due to Before, BeforeOrAt, After, AfterOrAt method.
    /// </remarks>
    public class LrcFile : ILrcFile
    {
        private readonly IOneLineLyric[] _lyrics;
        /// <summary>
        /// Gets the metadata.
        /// </summary>
        /// <value>
        /// The metadata.
        /// </value>
        public ILrcMetadata Metadata { get; private set; }
        /// <summary>
        /// Gets the lyrics.
        /// </summary>
        /// <value>
        /// The lyrics.
        /// </value>
        public IList<IOneLineLyric> Lyrics { get { return _lyrics; } }

        private readonly OneLineLyricComparer _comparer = new OneLineLyricComparer();

        /// <summary>
        /// Gets the one line lyric content with the specified timestamp.
        /// </summary>
        /// <value>
        /// The one line lyric content.
        /// </value>
        /// <param name="timestamp">The timestamp.</param>
        /// <returns></returns>
        public string this[TimeSpan timestamp]
        {
            get
            {
                var index = Array.BinarySearch(_lyrics, new OneLineLyric(timestamp, null), _comparer);
                return index >= 0 ? _lyrics[index].Content : null;
            }
        }

        /// <summary>
        /// Gets the <see cref="IOneLineLyric"/> at the specified index.
        /// </summary>
        /// <value>
        /// The <see cref="IOneLineLyric"/>.
        /// </value>
        /// <param name="index">The index.</param>
        /// <returns></returns>
        public IOneLineLyric this[int index]
        {
            get { return _lyrics[index]; }
        }

        /// <summary>
        /// Gets the <see cref="IOneLineLyric" /> before the specified timestamp.
        /// </summary>
        /// <param name="timestamp">The timestamp.</param>
        /// <returns></returns>
        public IOneLineLyric Before(TimeSpan timestamp)
        {
            var index = Array.BinarySearch(_lyrics, new OneLineLyric(timestamp, null), _comparer);
            if (index >= 0)
            {
                return index > 0 ? _lyrics[index - 1] : null;
            }
            index = ~index;
            return index > 0 ? _lyrics[index - 1] : null;
        }

        /// <summary>
        /// Gets the <see cref="IOneLineLyric" /> before or at the specified timestamp.
        /// </summary>
        /// <param name="timestamp">The timestamp.</param>
        /// <returns></returns>
        public IOneLineLyric BeforeOrAt(TimeSpan timestamp)
        {
            var index = Array.BinarySearch(_lyrics, new OneLineLyric(timestamp, null), _comparer);
            if (index >= 0) return _lyrics[index];
            index = ~index;
            return index > 0 ? _lyrics[index - 1] : null;
        }

        /// <summary>
        /// Gets the <see cref="IOneLineLyric" /> after the specified timestamp.
        /// </summary>
        /// <param name="timestamp">The timestamp.</param>
        /// <returns></returns>
        public IOneLineLyric After(TimeSpan timestamp)
        {
            var index = Array.BinarySearch(_lyrics, new OneLineLyric(timestamp, null), _comparer);
            if (index >= 0)
            {
                return index + 1 < _lyrics.Length ? _lyrics[index + 1] : null;
            }
            index = ~index;
            return index < _lyrics.Length ? _lyrics[index] : null;
        }

        /// <summary>
        /// Gets the <see cref="IOneLineLyric" /> after or at the specified timestamp.
        /// </summary>
        /// <param name="timestamp">The timestamp.</param>
        /// <returns></returns>
        public IOneLineLyric AfterOrAt(TimeSpan timestamp)
        {
            var index = Array.BinarySearch(_lyrics, new OneLineLyric(timestamp, null), _comparer);
            if (index >= 0) return _lyrics[index];
            index = ~index;
            return index < _lyrics.Length ? _lyrics[index] : null;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LrcFile"/> class.
        /// </summary>
        /// <param name="metadata">The metadata.</param>
        /// <param name="lyrics">The lyrics.</param>
        /// <param name="applyOffset">if set to <c>true</c> apply the offset in metadata, otherwise ignore the offset.</param>
        public LrcFile(ILrcMetadata metadata, IEnumerable<IOneLineLyric> lyrics, bool applyOffset)
        {
            if (metadata == null) throw new ArgumentNullException("metadata");
            if (lyrics == null) throw new ArgumentNullException("lyrics");
            Metadata = metadata;
            var list = lyrics.OrderBy(l => l.Timestamp).ToList();

            for (int i = list.Count - 1; i >= 0; i--)
            {
                if (applyOffset && metadata.Offset.HasValue)
                {
                    var oneLineLyric = list[i] as OneLineLyric;
                    if (oneLineLyric != null)
                    {
                        oneLineLyric.Timestamp -= metadata.Offset.Value;
                    }
                    else
                    {
                        list[i] = new OneLineLyric(list[i].Timestamp - metadata.Offset.Value, list[i].Content);
                    }
                }
                if (i < list.Count - 1 && list[i].Timestamp == list[i + 1].Timestamp)
                {
                    list.RemoveAt(i);
                }
            }

            _lyrics = list.ToArray();
        }

        private static readonly Regex TimestampRegex = new Regex(@"^(?'minutes'\d+):(?'seconds'\d+(\.\d+)?)$");
        private static readonly Regex MetadataRegex = new Regex(@"^(?'title'[A-Za-z]+?):(?'content'.*)$");

        /// <summary>
        /// Create a new new instance of the <see cref="ILrcFile"/> interface with the specified LRC text.
        /// </summary>
        /// <param name="lrcText">The LRC text.</param>
        /// <returns></returns>
        public static ILrcFile FromText(string lrcText)
        {
            if (lrcText == null) throw new ArgumentNullException("lrcText");
            var pairs = new List<KeyValuePair<string, string>>();
            var titles = new List<string>();
            var sb = new StringBuilder();

            // 0: Line start. Expect line ending or [.
            // 1: Reading title. Expect ] or all characters except line ending.
            // 2: Reading content. Expect line ending or [ or other charactors.
            var state = 0;
            for (var i = 0; i <= lrcText.Length; ++i)
            {
                var ended = i >= lrcText.Length;
                var ch = ended ? (char)0 : lrcText[i];
                // ReSharper disable once IdentifierTypo
                var unescaped = false;
                if (ch == '\\')
                {
                    ++i;
                    ended = i >= lrcText.Length;
                    if (ended)
                    {
                        throw new FormatException("Expect one charactor after '\\' but reaches the end.");
                    }
                    ch = lrcText[i];
                    unescaped = true;
                }

                switch (state)
                {
                    case 0:
                        if (!unescaped && ch == '[')
                        {
                            state = 1;
                        }
                        else if (!unescaped && (ch == '\r' || ch == '\n') || ended)
                        {
                            state = 0;
                        }
                        else
                        {
                            throw new FormatException(string.Format("Expect '[' at position {0}", i));
                        }
                        break;
                    case 1:
                        if (!unescaped && ch == ']')
                        {
                            state = 2;
                            titles.Add(sb.ToString());
                            sb.Clear();
                        }
                        else if (!unescaped && (ch == '\r' || ch == '\n') || ended)
                        {
                            // 只有'['没有']'，作为内容
                            goto case 2;

                            //throw new FormatException(string.Format("Expect ']' at position {0}", i));
                        }
                        else
                        {
                            sb.Append(ch); // append to title
                        }
                        break;
                    case 2:
                        if (!unescaped && (ch == '\r' || ch == '\n') || ended)
                        {
                            state = 0;
                            var content = sb.ToString();
                            pairs.AddRange(titles.Select(t => new KeyValuePair<string, string>(t, content)));
                            sb.Clear();
                            titles.Clear();
                        }
                        else if (!unescaped && ch == '[')
                        {
                            if (sb.Length > 0)
                            {
                                state = 1;
                                var content = sb.ToString();
                                pairs.AddRange(titles.Select(t => new KeyValuePair<string, string>(t, content)));
                                sb.Clear();
                                titles.Clear();
                            }
                            else
                            {
                                state = 1;
                            }
                        }
                        else
                        {
                            sb.Append(ch);
                        }
                        break;
                }
            }

            var lyrics = new List<IOneLineLyric>();
            var metadata = new LrcMetadata();
            string offsetString = null;
            foreach (var pair in pairs)
            {
                // Parse timestamp
                var match = TimestampRegex.Match(pair.Key);
                if (match.Success)
                {
                    var minutes = int.Parse(match.Groups["minutes"].Value, System.Globalization.CultureInfo.InvariantCulture);
                    var seconds = double.Parse(match.Groups["seconds"].Value, System.Globalization.CultureInfo.InvariantCulture);
                    var timestamp = TimeSpan.FromSeconds(minutes * 60 + seconds);
                    lyrics.Add(new OneLineLyric(timestamp, pair.Value));
                    continue;
                }

                // Parse metadata
                match = MetadataRegex.Match(pair.Key);
                if (match.Success)
                {
                    var title = match.Groups["title"].Value.ToLower();
                    var content = match.Groups["content"].Value;
                    if (title == "ti")
                    {
                        if (metadata.Title == null && content != metadata.Title)
                        {
                            //throw new FormatException(string.Format("Duplicate LRC metadata found. Metadata name: '{0}', Values: '{1}', '{2}'", "ti", metadata.Title, content));
                        }
                        metadata.Title = content;
                    }
                    else if (title == "ar")
                    {
                        if (metadata.Artist != null && content != metadata.Artist)
                        {
                            //throw new FormatException(string.Format("Duplicate LRC metadata found. Metadata name: '{0}', Values: '{1}', '{2}'", "ar", metadata.Artist, content));
                        }
                        metadata.Artist = content;
                    }
                    else if (title == "al")
                    {
                        if (metadata.Album != null && content != metadata.Album)
                        {
                            //throw new FormatException(string.Format("Duplicate LRC metadata found. Metadata name: '{0}', Values: '{1}', '{2}'", "al", metadata.Album, content));
                        }
                        metadata.Album = content;
                    }
                    else if (title == "by")
                    {
                        if (metadata.Maker != null && content != metadata.Maker)
                        {
                            //throw new FormatException(string.Format("Duplicate LRC metadata found. Metadata name: '{0}', Values: '{1}', '{2}'", "by", metadata.Maker, content));
                        }
                        metadata.Maker = content;
                    }
                    else if (title == "offset")
                    {
                        if (offsetString != null && content != offsetString)
                        {
                            //throw new FormatException(string.Format("Duplicate LRC metadata found. Metadata name: '{0}', Values: '{1}', '{2}'", "offset", offsetString, content));
                        }
                        offsetString = content;
                        metadata.Offset = TimeSpan.FromMilliseconds(double.Parse(content, System.Globalization.CultureInfo.InvariantCulture));
                    }
                    // ReSharper disable once RedundantIfElseBlock
                    else
                    {
                        // Ingore unsupported tag
                    }
                }
                else
                {
                    //throw new FormatException(string.Format("Unknown tag [{0}]", pair.Key));
                }
            }

            if (lyrics.Count == 0)
            {
                throw new FormatException("Invalid or empty LRC text. Can't find any lyrics.");
            }
            return new LrcFile(metadata, lyrics, true);
        }
    }
}
