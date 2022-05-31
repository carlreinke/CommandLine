// Copyright 2022 Carl Reinke
//
// This file is part of a library that is licensed under the terms of GNU Lesser
// General Public License version 3 as published by the Free Software
// Foundation.
//
// This license does not grant rights under trademark law for use of any trade
// names, trademarks, or service marks.

using System.Diagnostics.CodeAnalysis;

namespace Tetractic.CommandLine
{
    /// <summary>
    /// Attempts to parse text to a value.
    /// </summary>
    /// <typeparam name="T">The type of value.</typeparam>
    /// <param name="text">The text to parse.</param>
    /// <param name="value">Returns the value that was parsed if parsing was successful; otherwise,
    ///     returns the uninitialized value.</param>
    /// <returns><see langword="true"/> is parsing was successful; otherwise,
    ///     <see langword="false"/>.</returns>
    public delegate bool TryParser<T>(string text, [MaybeNullWhen(false)] out T value);
}
