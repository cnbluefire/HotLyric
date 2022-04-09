#nullable disable

using System;
using System.Collections.Generic;

namespace Kfstorm.LrcParser
{
    /// <summary>
    /// Represents an LRC format lyrics file.
    /// </summary>
    /// <remarks>
    /// Duplicate timestamps is not supported due to Before, BeforeOrAt, After, AfterOrAt method.
    /// </remarks>
    public interface ILrcFile
    {
        /// <summary>
        /// Gets the metadata.
        /// </summary>
        /// <value>
        /// The metadata.
        /// </value>
        ILrcMetadata Metadata { get; }
        /// <summary>
        /// Gets the lyrics.
        /// </summary>
        /// <value>
        /// The lyrics.
        /// </value>
        IList<IOneLineLyric> Lyrics { get; }
        /// <summary>
        /// Gets the one line lyric content with the specified timestamp.
        /// </summary>
        /// <value>
        /// The one line lyric content.
        /// </value>
        /// <param name="timestamp">The timestamp.</param>
        /// <returns></returns>
        string this[TimeSpan timestamp] { get; }
        /// <summary>
        /// Gets the <see cref="IOneLineLyric"/> at the specified index.
        /// </summary>
        /// <value>
        /// The <see cref="IOneLineLyric"/>.
        /// </value>
        /// <param name="index">The index.</param>
        /// <returns></returns>
        IOneLineLyric this[int index] { get; }
        /// <summary>
        /// Gets the <see cref="IOneLineLyric"/> before the specified timestamp.
        /// </summary>
        /// <param name="timestamp">The timestamp.</param>
        /// <returns></returns>
        IOneLineLyric Before(TimeSpan timestamp);
        /// <summary>
        /// Gets the <see cref="IOneLineLyric"/> before or at the specified timestamp.
        /// </summary>
        /// <param name="timestamp">The timestamp.</param>
        /// <returns></returns>
        IOneLineLyric BeforeOrAt(TimeSpan timestamp);
        /// <summary>
        /// Gets the <see cref="IOneLineLyric"/> after the specified timestamp.
        /// </summary>
        /// <param name="timestamp">The timestamp.</param>
        /// <returns></returns>
        IOneLineLyric After(TimeSpan timestamp);
        /// <summary>
        /// Gets the <see cref="IOneLineLyric"/> after or at the specified timestamp.
        /// </summary>
        /// <param name="timestamp">The timestamp.</param>
        /// <returns></returns>
        IOneLineLyric AfterOrAt(TimeSpan timestamp);
    }
}
