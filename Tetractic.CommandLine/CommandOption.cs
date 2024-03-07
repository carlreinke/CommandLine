// Copyright 2024 Carl Reinke
//
// This file is part of a library that is licensed under the terms of the GNU
// Lesser General Public License Version 3 as published by the Free Software
// Foundation.
//
// This license does not grant rights under trademark law for use of any trade
// names, trademarks, or service marks.

using System;

namespace Tetractic.CommandLine
{
    /// <summary>
    /// Represents a command option.
    /// </summary>
    public class CommandOption
    {
        /// <exception cref="ArgumentException"><paramref name="longName"/> is
        ///     <see langword="null"/> and <paramref name="shortName"/> is <see langword="null"/>.
        ///     </exception>
        /// <exception cref="ArgumentNullException"><paramref name="description"/> is
        ///     <see langword="null"/>.</exception>
        internal CommandOption(char? shortName, string? longName, string description, bool inherited)
        {
            if (longName is null && shortName is null)
                throw new ArgumentException("No names were specified.");
            if (description is null)
                throw new ArgumentNullException(nameof(description));

            LongName = longName;
            ShortName = shortName;
            Description = description;
            Inherited = inherited;
        }

        /// <summary>
        /// Gets the long name of the command option.
        /// </summary>
        /// <value>The long name of the command option.</value>
        public string? LongName { get; }

        /// <summary>
        /// Gets the short name of the command option.
        /// </summary>
        /// <value>The short name of the command option.</value>
        public char? ShortName { get; }

        /// <summary>
        /// Gets the description of the command option.
        /// </summary>
        /// <value>The description of the command option.</value>
        public string Description { get; }

        /// <summary>
        /// Gets a value indicating whether the command option can store multiple values.
        /// </summary>
        /// <value>A value indicating whether the command option can store multiple values.</value>
        public virtual bool Variadic => false;

        /// <summary>
        /// Gets or sets a value indicating whether the command option must be specified.
        /// </summary>
        /// <value>A value indicating whether the command option must be specified.</value>
        public bool Required { get; set; }

        /// <summary>
        /// Gets a value indicating whether the command option is inherited by subcommands.
        /// </summary>
        /// <value>A value indicating whether the command option is inherited by subcommands.
        ///     </value>
        public bool Inherited { get; }

        /// <summary>
        /// Gets or sets a value indicating when the command option appears in the help text for the
        /// command.
        /// </summary>
        /// <value>A value indicating when the command option appears in the help text for the
        ///     command.</value>
        public HelpVisibility HelpVisibility { get; set; }

        /// <summary>
        /// Gets the number of times that the command option has been accepted.
        /// </summary>
        /// <value>The number of times that the command option has been accepted.</value>
        public int Count { get; protected set; }

        /// <summary>
        /// Accepts the command option without a value.
        /// </summary>
        /// <exception cref="InvalidOperationException">The command option parameter is not
        ///     optional.</exception>
        public virtual void Accept()
        {
            checked { Count += 1; }
        }

        /// <summary>
        /// Attempts to parse specified text to a value and stores the value into the command
        /// option.
        /// </summary>
        /// <param name="text">The text to parse.</param>
        /// <returns><see langword="true"/> if the text was parsed to a value; otherwise,
        ///     <see langword="false"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="text"/> is
        ///     <see langword="null"/>.</exception>
        /// <exception cref="InvalidOperationException">The command option does not have a
        ///     parameter.</exception>
        /// <remarks>
        /// If <see cref="Variadic"/> is <see langword="false"/> then the value replaces the
        /// existing value, if any.  If <see cref="Variadic"/> is <see langword="true"/> then the
        /// value is appended to the list of values for the command option.  In either case,
        /// <see cref="Count"/> is incremented each time <see cref="TryAcceptValue(string)"/>
        /// returns <see langword="true"/>.
        /// </remarks>
        public virtual bool TryAcceptValue(string text)
        {
            if (text is null)
                throw new ArgumentNullException(nameof(text));

            throw new InvalidOperationException("The command option does not expect a value.");
        }

        internal virtual void Reset()
        {
            Count = 0;
        }
    }
}
