#nullable disable

using System;

namespace Kfstorm.LrcParser
{
    /// <summary>
    /// Represents an one line lyric.
    /// </summary>
    public interface IOneLineLyric
    {
        /// <summary>
        /// Gets the timestamp.
        /// </summary>
        /// <value>
        /// The timestamp.
        /// </value>
        TimeSpan Timestamp { get; }
        /// <summary>
        /// Gets the content.
        /// </summary>
        /// <value>
        /// The content.
        /// </value>
        string Content { get; }
    }
}
