// Copyright 2024 Carl Reinke
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
    /// Represents a command option that stores a value or multiple values without respect to value
    /// type.
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
        /// <value>A value indicating whether the command option parameter is optional.</value>
        public bool ParameterIsOptional { get; private protected set; }

        /// <summary>
        /// Gets the name of the command option parameter.
        /// </summary>
        /// <value>The name of the command option parameter.</value>
        public string ParameterName { get; }

        /// <summary>
        /// Gets or sets the provider of completions for the parameter value.
        /// </summary>
        public ICompletionProvider? ParameterCompletionProvider { get; set; }
    }
}
