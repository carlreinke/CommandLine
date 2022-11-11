// Copyright 2022 Carl Reinke
//
// This file is part of a library that is licensed under the terms of the GNU
// Lesser General Public License Version 3 as published by the Free Software
// Foundation.
//
// This license does not grant rights under trademark law for use of any trade
// names, trademarks, or service marks.

using System;
using System.Collections;
using System.Collections.Generic;

namespace Tetractic.CommandLine
{
    public sealed partial class VariadicCommandParameter<T>
    {
        /// <summary>
        /// A read-only list of command parameter values.
        /// </summary>
        public readonly struct ValueList : IReadOnlyList<T>
        {
            private readonly List<T> _values;

            internal ValueList(List<T> values)
            {
                _values = values;
            }

            /// <summary>
            /// Gets the number of values in the list.
            /// </summary>
            /// <value>The number of values in the list.</value>
            public int Count => _values is null ? 0 : _values.Count;

            /// <summary>
            /// Gets the value at a specified index.
            /// </summary>
            /// <param name="index">The index of the value to get.</param>
            /// <returns>The value at the specified index.</returns>
            /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is less than
            ///     zero or is greater than or equal to <see cref="Count"/>.</exception>
            public T this[int index]
            {
                get
                {
                    if (_values is null)
                        throw new ArgumentOutOfRangeException(nameof(index));

                    try
                    {
                        return _values[index];
                    }
                    catch (ArgumentOutOfRangeException)
                    {
                        throw new ArgumentOutOfRangeException(nameof(index));
                    }
                }
            }

            /// <summary>
            /// Gets an enumerator over the values in the list.
            /// </summary>
            /// <returns>An enumerator over the values in the list.</returns>
            public IEnumerator<T> GetEnumerator()
            {
                if (_values is null)
                    return ((IList<T>)Array.Empty<T>()).GetEnumerator();

                return _values.GetEnumerator();
            }

            /// <inheritdoc cref="GetEnumerator"/>
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }
    }
}
