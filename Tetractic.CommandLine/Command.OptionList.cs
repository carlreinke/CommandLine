// Copyright 2022 Carl Reinke
//
// This file is part of a library that is licensed under the terms of GNU Lesser
// General Public License version 3 as published by the Free Software
// Foundation.
//
// This license does not grant rights under trademark law for use of any trade
// names, trademarks, or service marks.

using System;
using System.Collections;
using System.Collections.Generic;

namespace Tetractic.CommandLine
{
    public partial class Command
    {
        /// <summary>
        /// A read-only list of command options.
        /// </summary>
        public readonly struct OptionList : IReadOnlyList<CommandOption>
        {
            private readonly List<CommandOption> _list;

            internal OptionList(List<CommandOption> options)
            {
                _list = options;
            }

            /// <summary>
            /// Gets the number of command options in the list.
            /// </summary>
            public int Count => _list is null ? 0 : _list.Count;

            /// <summary>
            /// Gets the command option at a specified index.
            /// </summary>
            /// <param name="index">The index of the command option to get.</param>
            /// <returns>The command option at the specified index.</returns>
            /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is less than
            ///     zero or is greater than or equal to <see cref="Count"/>.</exception>
            public CommandOption this[int index]
            {
                get
                {
                    if (_list is null)
                        throw new ArgumentOutOfRangeException(nameof(index));

                    try
                    {
                        return _list[index];
                    }
                    catch (ArgumentOutOfRangeException)
                    {
                        throw new ArgumentOutOfRangeException(nameof(index));
                    }
                }
            }

            /// <summary>
            /// Gets an enumerator over the command options in the list.
            /// </summary>
            /// <returns>An enumerator over the command options in the list.</returns>
            public IEnumerator<CommandOption> GetEnumerator()
            {
                if (_list is null)
                    return ((IList<CommandOption>)Array.Empty<CommandOption>()).GetEnumerator();

                return _list.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }
    }
}
