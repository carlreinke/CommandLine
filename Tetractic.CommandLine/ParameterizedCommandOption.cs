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
    /// Represents a command option that stores a value or multiple values.
    /// </summary>
    public abstract class ParameterizedCommandOption : CommandOption
    {
        /// <exception cref="ArgumentException"><paramref name="longName"/> is
        ///     <see langword="null"/> and <paramref name="shortName"/> is <see langword="null"/>.
        ///     </exception>
        /// <exception cref="ArgumentNullException"><paramref name="parameterName"/> is
        ///     <see langword="null"/>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="description"/> is
        ///     <see langword="null"/>.</exception>
        internal ParameterizedCommandOption(char? shortName, string? longName, string parameterName, string description, bool inherited)
            : base(shortName, longName, description, inherited)
        {
            if (parameterName is null)
                throw new ArgumentNullException(nameof(parameterName));

            ParameterName = parameterName;
        }

        /// <summary>
        /// Gets a value indicating whether the command option parameter is optional.
        /// </summary>
        public bool ParameterIsOptional { get; private protected set; }

        /// <summary>
        /// Gets the name of the command option parameter.
        /// </summary>
        public string ParameterName { get; }
    }
}
