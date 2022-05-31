// Copyright 2022 Carl Reinke
//
// This file is part of a library that is licensed under the terms of GNU Lesser
// General Public License version 3 as published by the Free Software
// Foundation.
//
// This license does not grant rights under trademark law for use of any trade
// names, trademarks, or service marks.

using System;
using System.Diagnostics.CodeAnalysis;

namespace Tetractic.CommandLine
{
    /// <summary>
    /// Represents a command option that stores a value.
    /// </summary>
    /// <typeparam name="T">The type of value that the command option stores.</typeparam>
    public sealed class CommandOption<T> : ParameterizedCommandOption<T>
    {
        private readonly TryParser<T> _parse;

        private T _value = default!;

        /// <exception cref="ArgumentException"><paramref name="longName"/> is
        ///     <see langword="null"/> and <paramref name="shortName"/> is <see langword="null"/>.
        ///     </exception>
        /// <exception cref="ArgumentNullException"><paramref name="parameterName"/> is
        ///     <see langword="null"/>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="description"/> is
        ///     <see langword="null"/>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="parse"/> is
        ///     <see langword="null"/>.</exception>
        internal CommandOption(char? shortName, string? longName, string parameterName, string description, TryParser<T> parse, bool inherited)
            : base(shortName, longName, parameterName, description, inherited)
        {
            if (parse is null)
                throw new ArgumentNullException(nameof(parse));

            _parse = parse;
        }

        /// <summary>
        /// Gets a value indicating whether the command option can store multiple values.  Always
        /// returns <see langword="false"/>.
        /// </summary>
        public sealed override bool Variadic => false;

        /// <summary>
        /// Gets a value indicating whether a value has been stores into the command option.
        /// </summary>
        public bool HasValue => Count > 0;

        /// <summary>
        /// Gets the value that was stored into the command option.
        /// </summary>
        /// <exception cref="InvalidOperationException">The command option does not have a value.
        ///     </exception>
        public T Value
        {
            get
            {
                if (Count == 0)
                    throw new InvalidOperationException("The command option does not have a value.");

                return _value;
            }
        }

        /// <summary>
        /// Gets the value that was stored into the command option if the command option has a
        /// value; otherwise, gets the uninitialized value.
        /// </summary>
        [MaybeNull]
        public T ValueOrDefault => _value;

        /// <summary>
        /// Stores <see cref="ParameterizedCommandOption{T}.OptionalParameterDefaultValue"/> into
        /// the command option if the command option parameter is optional.
        /// </summary>
        /// <exception cref="InvalidOperationException">The command option parameter is not
        ///     optional.</exception>
        public override void Accept()
        {
            if (!ParameterIsOptional)
                throw new InvalidOperationException("The command option requires a value.");

            checked { Count += 1; }
            _value = _optionalParameterDefaultValue;
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
        /// <remarks>
        /// Only the last value stored into the command option is retained, but
        /// <see cref="CommandParameter.Count"/> is incremented each time
        /// <see cref="TryAcceptValue(string)"/> returns <see langword="true"/>.
        /// </remarks>
        public override bool TryAcceptValue(string text)
        {
            if (text is null)
                throw new ArgumentNullException(nameof(text));

            if (_parse(text, out var value))
            {
                checked { Count += 1; }
                _value = value;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Returns the value that was stored into the command option if the command option has a
        /// value; otherwise, returns a specified value.
        /// </summary>
        /// <param name="default">The value to return if the command option does not have a value.
        ///     </param>
        /// <returns>The value that was stored into the command option if the command option has a
        ///     value; otherwise, <paramref name="default"/>.</returns>
        public T GetValueOrDefault(T @default) => HasValue ? _value : @default;
    }
}
