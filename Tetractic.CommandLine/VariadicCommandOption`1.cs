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
    /// Represents a command option that stores multiple values.
    /// </summary>
    /// <typeparam name="T">The type of values that the command option stores.</typeparam>
    public sealed partial class VariadicCommandOption<T> : ParameterizedCommandOption<T>
    {
        private readonly TryParser<T> _parse;

        private readonly List<T> _values;

        /// <exception cref="ArgumentException"><paramref name="longName"/> is
        ///     <see langword="null"/> and <paramref name="shortName"/> is <see langword="null"/>.
        ///     </exception>
        /// <exception cref="ArgumentNullException"><paramref name="parameterName"/> is
        ///     <see langword="null"/>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="description"/> is
        ///     <see langword="null"/>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="parse"/> is
        ///     <see langword="null"/>.</exception>
        internal VariadicCommandOption(char? shortName, string? longName, string parameterName, string description, TryParser<T> parse, bool inherited)
            : base(shortName, longName, parameterName, description, inherited)
        {
            if (parse is null)
                throw new ArgumentNullException(nameof(parse));

            _parse = parse;
            _values = new List<T>();
        }

        /// <summary>
        /// Gets a value indicating whether the command option can store multiple values.  Always
        /// returns <see langword="true"/>.
        /// </summary>
        public sealed override bool Variadic => true;

        /// <summary>
        /// Gets the list of values that have been stored into the command option.
        /// </summary>
        public ValueList Values => new ValueList(_values);

        /// <summary>
        /// Appends <see cref="ParameterizedCommandOption{T}.OptionalParameterDefaultValue"/> to the
        /// list of values stored into the command option if the command option parameter is
        /// optional.
        /// </summary>
        /// <exception cref="InvalidOperationException">The command option parameter is not
        ///     optional.</exception>
        public override void Accept()
        {
            if (!ParameterIsOptional)
                throw new InvalidOperationException("The command option requires a value.");

            checked { Count += 1; }
            _values.Add(_optionalParameterDefaultValue);
        }

        /// <summary>
        /// Attempts to parse specified text to a value and appends it to the list of values stored
        /// into the command option.
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
