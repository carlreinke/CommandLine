// Copyright 2024 Carl Reinke
//
// This file is part of a library that is licensed under the terms of the GNU
// Lesser General Public License Version 3 as published by the Free Software
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
        /// Gets a value indicating whether specifying the command option multiple times has a
        /// different effect than specifying it once.
        /// </summary>
        /// <value>Always <see langword="false"/>.</value>
        public sealed override bool Variadic => false;

        /// <summary>
        /// Gets a value indicating whether a value has been stored into the command option.
        /// </summary>
        /// <value>A value indicating whether a value has been stored into the command option.
        ///     </value>
        public bool HasValue => Count > 0;

        /// <summary>
        /// Gets the value that was stored into the command option.
        /// </summary>
        /// <value>The value that was stored into the command option.</value>
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
        /// value; otherwise, gets the default value for <typeparamref name="T"/>.
        /// </summary>
        /// <value>The value that was stored into the command option if the command option has a
        ///     value; otherwise, the default value for <typeparamref name="T"/>.</value>
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

        internal override void Reset()
        {
            Count = 0;
            _value = default!;
        }
    }
}
