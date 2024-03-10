// Copyright 2024 Carl Reinke
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
    /// <summary>
    /// A completion provider.
    /// </summary>
#pragma warning disable CA1710 // Identifiers should have correct suffix
    public sealed class CompletionProvider : ICompletionProvider, IEnumerable<Completion>
#pragma warning restore CA1710 // Identifiers should have correct suffix
    {
        private readonly StringComparison _comparison;

        private readonly List<Completion> _completions = new List<Completion>();

        /// <summary>
        /// Initializes a new instance of <see cref="CompletionProvider"/>.
        /// </summary>
        /// <param name="comparison">The string comparison to use in determining whether to provide
        ///     a completion.</param>
        /// <exception cref="ArgumentException"><paramref name="comparison"/> is invalid.
        ///     </exception>
        public CompletionProvider(StringComparison comparison)
        {
            if ((uint)comparison > (uint)StringComparison.OrdinalIgnoreCase)
                throw new ArgumentException("Invalid value.", nameof(comparison));

            _comparison = comparison;
        }

        /// <summary>
        /// Adds a completion to the provider.
        /// </summary>
        /// <param name="completion">The completion to add.</param>
        public void Add(Completion completion)
        {
            _completions.Add(completion);
        }

        /// <inheritdoc/>
        public IEnumerable<Completion> GetCompletions(string text)
        {
            foreach (var completion in _completions)
                if (completion.Text.StartsWith(text, _comparison))
                    yield return completion;
        }

        IEnumerator<Completion> IEnumerable<Completion>.GetEnumerator() => _completions.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => _completions.GetEnumerator();
    }
}
