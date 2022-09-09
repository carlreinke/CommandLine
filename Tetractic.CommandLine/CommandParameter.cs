// Copyright 2022 Carl Reinke
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
    /// Represents a command parameter.
    /// </summary>
    public abstract class CommandParameter
    {
        /// <exception cref="ArgumentNullException"><paramref name="name"/> is
        ///     <see langword="null"/>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="description"/> is
        ///     <see langword="null"/>.</exception>
        private protected CommandParameter(string name, string description)
        {
            if (name is null)
                throw new ArgumentNullException(nameof(name));
            if (description is null)
                throw new ArgumentNullException(nameof(description));

            Name = name;
            Description = description;
        }

        /// <summary>
        /// Gets the name of the command parameter.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the description of the command parameter.
        /// </summary>
        public string Description { get; }

        /// <summary>
        /// Gets a value indicating whether the command parameter can store multiple values.
        /// </summary>
        public abstract bool Variadic { get; }

        /// <summary>
        /// Gets or sets a value indicating whether the command parameter and all subsequent command
        /// parameters may be omitted.
        /// </summary>
        public bool Optional { get; set; }

        /// <summary>
        /// Gets or sets a value indicating when the command parameter appears in the help text for
        /// the command.
        /// </summary>
        public HelpVisibility HelpVisibility { get; set; }

        /// <summary>
        /// Gets the number of values that have been accepted into the command parameter.
        /// </summary>
        public int Count { get; protected set; }

        /// <summary>
        /// Attempts to parse specified text to a value and stores the value into the command
        /// parameter.
        /// </summary>
        /// <param name="text">The text to parse.</param>
        /// <returns><see langword="true"/> if the text was parsed to a value; otherwise,
        ///     <see langword="false"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="text"/> is
        ///     <see langword="null"/>.</exception>
        /// <remarks>
        /// If <see cref="Variadic"/> is <see langword="false"/> then the value replaces the
        /// existing value, if any.  If <see cref="Variadic"/> is <see langword="true"/> then the
        /// value is appended to the list of values for the command parameter.  In either case,
        /// <see cref="Count"/> is incremented each time <see cref="TryAcceptValue(string)"/>
        /// returns <see langword="true"/>.
        /// </remarks>
        public abstract bool TryAcceptValue(string text);
    }
}
