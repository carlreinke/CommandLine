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
    /// Provides extension methods for command parameters.
    /// </summary>
    public static class CommandParameterExtensions
    {
        /// <summary>
        /// Returns the value that was stored into the command parameter if the command parameter
        /// has a value; otherwise, returns a <see langword="null"/>.
        /// </summary>
        /// <returns>The value that was stored into the command parameter if the command parameter
        ///     has a value; otherwise, <see langword="null"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="parameter"/> is
        ///     <see langword="null"/>.</exception>
        public static T? GetValueOrNull<T>(this CommandParameter<T> parameter)
            where T : struct
        {
            if (parameter is null)
                throw new ArgumentNullException(nameof(parameter));

            return parameter.HasValue ? parameter.ValueOrDefault : (T?)null;
        }
    }
}
