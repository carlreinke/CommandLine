// Copyright 2024 Carl Reinke
//
// This file is part of a library that is licensed under the terms of the GNU
// Lesser General Public License Version 3 as published by the Free Software
// Foundation.
//
// This license does not grant rights under trademark law for use of any trade
// names, trademarks, or service marks.

using System.Collections.Generic;

namespace Tetractic.CommandLine
{
    /// <summary>
    /// Provides command-line completions.
    /// </summary>
    public interface ICompletionProvider
    {
        /// <summary>
        /// Gets the completions for a specified incomplete text.
        /// </summary>
        /// <param name="text">The incomplete text.</param>
        /// <returns>The relevant completions.</returns>
        IEnumerable<Completion> GetCompletions(string text);
    }
}
