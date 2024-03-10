// Copyright 2024 Carl Reinke
//
// This file is part of a library that is licensed under the terms of the GNU
// Lesser General Public License Version 3 as published by the Free Software
// Foundation.
//
// This license does not grant rights under trademark law for use of any trade
// names, trademarks, or service marks.

using System;
using System.Diagnostics;

namespace Tetractic.CommandLine
{
    /// <summary>
    /// Represents a command-line completion.
    /// </summary>
    [DebuggerDisplay("{" + nameof(Text) + "}")]
    public readonly struct Completion
    {
        private readonly string? _text;

        /// <summary>
        /// Initializes a new instance of <see cref="Completion"/>.
        /// </summary>
        /// <param name="text">The completed text.</param>
        /// <param name="description">A description of the completion or <see langword="null"/> if
        ///     no description is available.</param>
        /// <exception cref="ArgumentNullException"><paramref name="text"/> is
        ///     <see langword="null"/>.</exception>
        public Completion(string text, string? description = null)
        {
            if (text is null)
                throw new ArgumentNullException(nameof(text));

            _text = text;
            Description = description;
        }

        /// <summary>
        /// Gets the completed text.
        /// </summary>
        /// <value>The completed text.</value>
        public string Text => _text ?? "";

        /// <summary>
        /// Gets an optional description of the completion.
        /// </summary>
        /// <value>A description of the completion or <see langword="null"/> if no description is
        ///     available.</value>
        public string? Description { get; }
    }
}
