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
    public partial class Command
    {
        /// <summary>
        /// A read-only list of subcommands.
        /// </summary>
        public readonly struct CommandList : IReadOnlyList<Command>
        {
            private readonly List<Command> _list;

            internal CommandList(List<Command> commands)
            {
                _list = commands;
            }

            /// <summary>
            /// Gets the number of subcommands in the list.
            /// </summary>
            public int Count => _list is null ? 0 : _list.Count;

            /// <summary>
            /// Gets the subcommand at a specified index in the list.
            /// </summary>
            /// <param name="index">The index of the subcommand to get.</param>
            /// <returns>The subcommand at the specified index in the list.</returns>
            /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is less than
            ///     zero or is greater than or equal to <see cref="Count"/>.</exception>
            public Command this[int index]
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
            /// Gets an enumerator over the subcommands in the list.
            /// </summary>
            /// <returns>An enumerator over the subcommands in the list.</returns>
            public IEnumerator<Command> GetEnumerator()
            {
                if (_list is null)
                    return ((IList<Command>)Array.Empty<Command>()).GetEnumerator();

                return _list.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            internal bool Exists(Predicate<Command> match) => _list.Exists(match);
        }
    }
}
