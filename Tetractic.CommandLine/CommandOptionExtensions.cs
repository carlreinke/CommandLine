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
    /// Provides extension methods for command options.
    /// </summary>
    public static class CommandOptionExtensions
    {
        /// <summary>
        /// Returns the value that was stored into the command option if the command option has a
        /// value; otherwise, returns <see langword="null"/>.
        /// </summary>
        /// <param name="option">The command option.</param>
        /// <returns>The value that was stored into the command option if the command option has a
        ///     value; otherwise, <see langword="null"/>.</returns>
        /// <typeparam name="T">The type of value that the command option stores.</typeparam>
        /// <exception cref="ArgumentNullException"><paramref name="option"/> is
        ///     <see langword="null"/>.</exception>
        public static T? GetValueOrNull<T>(this CommandOption<T> option)
            where T : struct
        {
            if (option is null)
                throw new ArgumentNullException(nameof(option));

            return option.HasValue ? option.ValueOrDefault : (T?)null;
        }
    }
}
