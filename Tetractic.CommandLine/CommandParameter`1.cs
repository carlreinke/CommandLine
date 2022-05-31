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
    /// Represents a command parameter that stores a value.
    /// </summary>
    /// <typeparam name="T">The type of value that the command parameter stores.</typeparam>
    public sealed class CommandParameter<T> : CommandParameter
    {
        private readonly TryParser<T> _parse;

        private T _value = default!;

        /// <exception cref="ArgumentNullException"><paramref name="name"/> is
        ///     <see langword="null"/>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="description"/> is
        ///     <see langword="null"/>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="parse"/> is
        ///     <see langword="null"/>.</exception>
        internal CommandParameter(string name, string description, TryParser<T> parse)
            : base(name, description)
        {
            if (parse is null)
                throw new ArgumentNullException(nameof(parse));

            _parse = parse;
        }

        /// <summary>
        /// Gets a value indicating whether the command parameter can store multiple values.  Always
        /// returns <see langword="false"/>.
        /// </summary>
        public sealed override bool Variadic => false;

        /// <summary>
        /// Gets a value indicating whether a value has been stores into the command parameter.
        /// </summary>
        public bool HasValue => Count > 0;

        /// <summary>
        /// Gets the value that was stored into the command parameter.
        /// </summary>
        /// <exception cref="InvalidOperationException">The command parameter does not have a value.
        ///     </exception>
        public T Value
        {
            get
            {
                if (Count == 0)
                    throw new InvalidOperationException("The command parameter does not have a value.");

                return _value;
            }
        }

        /// <summary>
        /// Gets the value that was stored into the command parameter if the command parameter has a
        /// value; otherwise, gets the uninitialized value.
        /// </summary>
        [MaybeNull]
        public T ValueOrDefault => _value;

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
        /// Only the last value stored into the command parameter is retained, but
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
        /// Returns the value that was stored into the command parameter if the command parameter
        /// has a value; otherwise, returns a specified value.
        /// </summary>
        /// <param name="default">The value to return if the command parameter does not have a
        ///     value.</param>
        /// <returns>The value that was stored into the command parameter if the command parameter
        ///     has a value; otherwise, <paramref name="default"/>.</returns>
        public T GetValueOrDefault(T @default) => HasValue ? _value : @default;
    }
}
