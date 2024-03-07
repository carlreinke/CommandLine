// Copyright 2024 Carl Reinke
//
// This file is part of a library that is licensed under the terms of the GNU
// Lesser General Public License Version 3 as published by the Free Software
// Foundation.
//
// This license does not grant rights under trademark law for use of any trade
// names, trademarks, or service marks.

namespace Tetractic.CommandLine
{
    /// <summary>
    /// Represents a command option that can be specified multiple times.
    /// </summary>
    public sealed class VariadicCommandOption : CommandOption
    {
        internal VariadicCommandOption(char? shortName, string? longName, string description, bool inherited)
            : base(shortName, longName, description, inherited)
        {
        }

        /// <summary>
        /// Gets a value indicating whether specifying the command option multiple times has a
        /// different effect than specifying it once.
        /// </summary>
        /// <value>Always <see langword="true"/>.</value>
        public override bool Variadic => true;
    }
}
