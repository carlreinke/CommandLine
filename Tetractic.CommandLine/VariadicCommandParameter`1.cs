// Copyright 2022 Carl Reinke
//
// This file is part of a library that is licensed under the terms of the GNU
// Lesser General Public License Version 3 as published by the Free Software
// Foundation.
//
// This license does not grant rights under trademark law for use of any trade
// names, trademarks, or service marks.

using System;
using System.Collections.Generic;

namespace Tetractic.CommandLine
{
    /// <summary>
    /// Represents a command parameter that stores multiple values.
    /// </summary>
    /// <typeparam name="T">The type of values that the command parameter stores.</typeparam>
    public sealed partial class VariadicCommandParameter<T> : CommandParameter
    {
        private readonly TryParser<T> _parse;

        private readonly List<T> _values;

        /// <exception cref="ArgumentNullException"><paramref name="parse"/> is
        ///     <see langword="null"/>.</exception>
        internal VariadicCommandParameter(string name, string description, TryParser<T> parse)
            : base(name, description)
        {
            if (parse is null)
                throw new ArgumentNullException(nameof(parse));

            _parse = parse;
            _values = new List<T>();
        }

        /// <summary>
        /// Gets a value indicating whether the command parameter can store multiple values.
        /// </summary>
        /// <value>Always <see langword="true"/>.</value>
        public sealed override bool Variadic => true;

        /// <summary>
        /// Gets the list of values that have been stored into the command parameter.
        /// </summary>
        /// <value>The list of values that have been stored into the command parameter.</value>
        public ValueList Values => new ValueList(_values);

        /// <summary>
        /// Gets or sets a value indicating whether wildcards will be expanded for the command
        /// parameter when running on Windows.
        /// </summary>
        /// <value>A value indicating whether wildcards will be expanded for the command parameter
        ///     when running on Windows.</value>
        /// <remarks>
        /// <para>
        /// When <see cref="ExpandWildcardsOnWindows"/> is <see langword="true"/> and the
        /// application is running on Windows, arguments containing wildcard characters will be
        /// expanded before being parsed.
        /// </para>
        /// <para>
        /// The wildcard characters are asterisk (*) and question mark (?).  An asterisk wildcard
        /// matches zero or more characters.  A question mark wildcard matches exactly one
        /// character.  All other characters are literals.
        /// </para>
        /// </remarks>
        public new bool ExpandWildcardsOnWindows
        {
            get => base.ExpandWildcardsOnWindows;
            set => base.ExpandWildcardsOnWindows = value;
        }

        /// <summary>
        /// Attempts to parse specified text to a value and appends it to the list of values stored
        /// into the command parameter.
        /// </summary>
        /// <param name="text">The text to parse.</param>
        /// <returns><see langword="true"/> if the text was parsed to a value; otherwise,
        ///     <see langword="false"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="text"/> is
        ///     <see langword="null"/>.</exception>
        public override bool TryAcceptValue(string text)
        {
            if (text is null)
                throw new ArgumentNullException(nameof(text));

            if (_parse(text, out var value))
            {
                checked { Count += 1; }
                _values.Add(value);
                return true;
            }

            return false;
        }
    }
}
