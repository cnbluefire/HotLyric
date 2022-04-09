#nullable disable

using System;

namespace Kfstorm.LrcParser
{
    /// <summary>
    /// Represents the metadata of an LRC format lyrics file.
    /// </summary>
    public class LrcMetadata : ILrcMetadata
    {
        /// <summary>
        /// Gets the title.
        /// </summary>
        /// <value>
        /// The title.
        /// </value>
        public string Title { get; set; }
        /// <summary>
        /// Gets the artist.
        /// </summary>
        /// <value>
        /// The artist.
        /// </value>
        public string Artist { get; set; }
        /// <summary>
        /// Gets the album.
        /// </summary>
        /// <value>
        /// The album.
        /// </value>
        public string Album { get; set; }
        /// <summary>
        /// Gets the lyrics maker.
        /// </summary>
        /// <value>
        /// The lyrics maker.
        /// </value>
        public string Maker { get; set; }
        /// <summary>
        /// Gets the offset.
        /// </summary>
        /// <value>
        /// The offset.
        /// </value>
        public TimeSpan? Offset { get; set; }
    }
}