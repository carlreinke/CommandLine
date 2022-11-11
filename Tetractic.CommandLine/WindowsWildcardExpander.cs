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
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Security;
using static Tetractic.CommandLine.WindowsWildcardExpander;

namespace Tetractic.CommandLine
{
    /// <summary>
    /// Provides enumeration of file system paths that match a pattern on Windows.
    /// </summary>
    internal static class WindowsWildcardExpander
    {
        private static readonly char[] _wildcards = new[] { '*', '?' };

        /// <summary>
        /// Gets a value indicating whether a pattern contains any wildcard characters.
        /// </summary>
        /// <param name="pattern">The pattern.</param>
        /// <returns><see langword="true"/> if <paramref name="pattern"/> contains any wildcard
        ///     characters.</returns>
        public static bool ContainsAnyWildcard(string pattern)
        {
            return pattern.IndexOfAny(_wildcards) >= 0;
        }

        /// <summary>
        /// Enumerates the file system paths that match a pattern.
        /// </summary>
        /// <param name="pattern">The pattern.  A relative pattern is evaluated</param>
        /// <returns>An enumerator over the patterns.</returns>
        /// <remarks>
        /// <para>
        /// A pattern is a sequence of literal and wildcard characters.  An asterisk (*) wildcard
        /// matches zero or more characters.  A question mark (?) wildcard matches exactly one
        /// character.  All other characters are literals.
        /// </para>
        /// <para>
        /// Relative patterns are evaluated against the working directory.  Changing the working
        /// directory during enumeration may cause unpredictable results.
        /// </para>
        /// <para>
        /// Patterns containing <c>..</c> segments can produce an exponential number of results (ex.
        /// <c>*/../*/../*</c>) or take an exponential amount of time without producing any results
        /// (ex. <c>*/../*/../*nonexistent</c>).  Consider carefully before evaluating patterns from
        /// untrusted input.
        /// </para>
        /// </remarks>
        public static IEnumerable<string> EnumerateMatches(string pattern) => WindowsWildcardExpander<Provider>.EnumerateMatches(pattern);

        // Dependency injection interface for testability.
        internal interface IDirectoryProvider
        {
            /// <inheritdoc cref="Directory.EnumerateDirectories(string)"/>
            IEnumerable<string> EnumerateDirectories(string path);

            /// <inheritdoc cref="Directory.EnumerateFileSystemEntries(string)"/>
            IEnumerable<string> EnumerateFileSystemEntries(string path);

            /// <inheritdoc cref="Directory.Exists(string)"/>
            bool Exists(string path);
        }

        // Dependency injection interface for testability.
        internal interface IFileProvider
        {
            /// <inheritdoc cref="File.Exists(string)"/>
            bool Exists(string path);
        }

        // Dependency injection interface for testability.
        internal interface IPathProvider
        {
            /// <inheritdoc cref="Path.AltDirectorySeparatorChar"/>
            char AltDirectorySeparatorChar { get; }

            /// <inheritdoc cref="Path.DirectorySeparatorChar"/>
            char DirectorySeparatorChar { get; }

            /// <inheritdoc cref="Path.InvalidPathChars"/>
            char[] InvalidPathChars { get; }

            /// <inheritdoc cref="Path.VolumeSeparatorChar"/>
            char VolumeSeparatorChar { get; }

            /// <exception cref="ArgumentException"/>
            string GetFileName(string path);
        }

        [ExcludeFromCodeCoverage]
        internal readonly struct Provider : IDirectoryProvider, IFileProvider, IPathProvider
        {
            private static readonly char[] _invalidPathChars = Path.GetInvalidPathChars();

            /// <inheritdoc/>
            char IPathProvider.AltDirectorySeparatorChar => Path.AltDirectorySeparatorChar;

            /// <inheritdoc/>
            char IPathProvider.DirectorySeparatorChar => Path.DirectorySeparatorChar;

            /// <inheritdoc/>
            char[] IPathProvider.InvalidPathChars => _invalidPathChars;

            /// <inheritdoc/>
            char IPathProvider.VolumeSeparatorChar => Path.VolumeSeparatorChar;

            /// <inheritdoc/>
            IEnumerable<string> IDirectoryProvider.EnumerateDirectories(string path) => Directory.EnumerateDirectories(path);

            /// <inheritdoc/>
            IEnumerable<string> IDirectoryProvider.EnumerateFileSystemEntries(string path) => Directory.EnumerateFileSystemEntries(path);

            /// <inheritdoc/>
            bool IDirectoryProvider.Exists(string path) => Directory.Exists(path);

            /// <inheritdoc/>
            bool IFileProvider.Exists(string path) => File.Exists(path);

            /// <inheritdoc/>
            string IPathProvider.GetFileName(string path) => Path.GetFileName(path);
        }
    }

    internal static class WindowsWildcardExpander<TProvider>
        where TProvider : struct, IDirectoryProvider, IFileProvider, IPathProvider
    {
        internal static IDirectoryProvider Directory => default(TProvider);

        internal static IFileProvider File => default(TProvider);

        internal static IPathProvider Path => default(TProvider);

        internal static IEnumerable<string> EnumerateMatches(string pattern)
        {
            var segments = Segment(pattern);

            if (segments.Count == 0)
            {
                yield return pattern;
                yield break;
            }

            bool any = false;

            var stack = new Stack<(int SegmentIndex, IEnumerator<string> Paths)>();

            int segmentIndex = 0;
            var pathEnumerator = GetInitialPathEnumerator();

            while (true)
            {
                if (!pathEnumerator.MoveNext())
                {
                    if (stack.Count == 0)
                        break;

                    pathEnumerator.Dispose();

                    (segmentIndex, pathEnumerator) = stack.Pop();

                    continue;
                }

                string path = pathEnumerator.Current;

                if (segmentIndex < segments.Count)
                {
                    var (literal, segmentPattern) = segments[segmentIndex];

                    if (literal.IndexOfAny(Path.InvalidPathChars) >= 0)
                        continue;

                    path += literal;

                    try
                    {
                        if (segmentPattern.Length == 0)
                        {
                            Debug.Assert(path.Length > 0);

                            if (!Directory.Exists(path) && !File.Exists(path))
                                continue;
                        }
                        else
                        {
                            string searchPath = path.Length == 0
                                ? "." + Path.DirectorySeparatorChar
                                : path;

                            if (!Directory.Exists(searchPath))
                                continue;

                            stack.Push((segmentIndex, pathEnumerator));

                            segmentIndex += 1;
                            pathEnumerator = EmptyEnumerator.Instance;

                            // Directory.Enumerate...(string path, string searchPattern) has archaic
                            // file extension and wildcard behaviors, which we don't want.
                            // (Ex. "*.abc" matches "x.abcd", "a." matches "a", "a?" matches "a",
                            // "a.*" matches "a", "a.?" matches "a", etc.)  So we have to do
                            // pattern matching manually.

                            var children = segmentIndex == segments.Count
                                ? Directory.EnumerateFileSystemEntries(searchPath)
                                : Directory.EnumerateDirectories(searchPath);

                            var names = new List<string>();
                            foreach (string child in children)
                                names.Add(Path.GetFileName(child));

                            names.Sort(StringComparer.OrdinalIgnoreCase);

                            pathEnumerator = new FilteredPathEnumerator(names.GetEnumerator(), path, segmentPattern);
                            continue;
                        }
                    }
                    catch (Exception ex)
                        when (ex is DirectoryNotFoundException ||
                              ex is PathTooLongException ||
                              ex is IOException ||
                              ex is SecurityException ||
                              ex is UnauthorizedAccessException)
                    {
                        continue;
                    }
                }

                any = true;
                yield return path;
            }

            if (!any)
                yield return pattern;

            static IEnumerator<string> GetInitialPathEnumerator()
            {
                yield return string.Empty;
            }
        }

        /// <summary>
        /// Splits a pattern into literal and pattern segments.
        /// </summary>
        /// <remarks>
        /// Literal segments may contain any number of directory/volume separator characters.  Each
        /// literal segment begins with a directory/volume separator character, except for the first
        /// segment, which may or may not.  Each literal segment ends with a directory/volume
        /// separator, except for the last segment, which may or may not.  Pattern segments do not
        /// contain directory/volume separator characters.
        /// </remarks>
        private static List<(string Literal, string Pattern)> Segment(string pattern)
        {
            var segments = new List<(string Literal, string Pattern)>();

            int lit = 0;  // literal segment offset
            int cur = 0;  // current segment offset

            for (int i = 0; ;)
            {
                if (i == pattern.Length)
                {
                    if (i != lit)
                        segments.Add((pattern.Substring(lit, i - lit), string.Empty));

                    break;
                }

                char c = pattern[i];

                if (c == Path.DirectorySeparatorChar ||
                    c == Path.AltDirectorySeparatorChar ||
                    c == Path.VolumeSeparatorChar)
                {
                    cur = i + 1;
                }
                else if (c == '*' ||
                         c == '?')
                {
                    i += 1;

                    for (; ; )
                    {
                        if (i == pattern.Length)
                        {
                            break;
                        }

                        c = pattern[i];

                        if (c == Path.DirectorySeparatorChar ||
                            c == Path.AltDirectorySeparatorChar ||
                            c == Path.VolumeSeparatorChar)
                        {
                            break;
                        }

                        i += 1;
                    }

                    segments.Add((pattern.Substring(lit, cur - lit), pattern.Substring(cur, i - cur)));

                    lit = i;
                    continue;
                }

                i += 1;
            }

            return segments;
        }

        /// <summary>
        /// Returns a value indicating whether a name matches a pattern.
        /// </summary>
        private static bool Match(string name, string pattern)
        {
            int n = 0;
            int p = 0;
            int backtrackN = name.Length;
            int backtrackP = 0;

            while (true)
            {
                if (p < pattern.Length)
                {
                    char c = pattern[p];
                    if (c == '*')
                    {
                        // '*' matches zero characters this time.
                        backtrackN = n;
                        p += 1;
                        backtrackP = p;
                        continue;
                    }
                    else if (n < name.Length && (c == '?' || EqualsOrdinalIgnoreCase(c, name[n])))
                    {
                        n += 1;
                        p += 1;
                        continue;
                    }
                }
                else if (n == name.Length)
                {
                    return true;
                }

                // Mismatch.  Try to backtrack.
                if (backtrackN < name.Length)
                {
                    // '*' matches one more character this time.
                    backtrackN += 1;
                    n = backtrackN;
                    p = backtrackP;
                    continue;
                }

                return false;
            }

            // There is no API to get the case mapping table for a volume, so
            // this is the best we can do.
            static bool EqualsOrdinalIgnoreCase(char c1, char c2)
            {
                return c1 == c2 || char.ToUpperInvariant(c1) == char.ToUpperInvariant(c2);
            }
        }

        private sealed class FilteredPathEnumerator : IEnumerator<string>
        {
            private readonly IEnumerator<string> _nameEnumerator;
            private readonly string _path;
            private readonly string _pattern;

            public FilteredPathEnumerator(IEnumerator<string> nameEnumerator, string path, string pattern)
            {
                _nameEnumerator = nameEnumerator;
                _path = path;
                _pattern = pattern;
                Current = null!;
            }

            public string Current { get; private set; }

            object IEnumerator.Current => Current;

            public void Dispose() => _nameEnumerator.Dispose();

            public bool MoveNext()
            {
            skip:
                if (!_nameEnumerator.MoveNext())
                    return false;

                string name = _nameEnumerator.Current;

                if (!Match(name, _pattern))
                    goto skip;

                Current = _path + name;
                return true;
            }

            /// <exception cref="NotSupportedException"/>
            public void Reset() => _nameEnumerator.Reset();
        }

        private sealed class EmptyEnumerator : IEnumerator<string>
        {
            public static readonly EmptyEnumerator Instance = new EmptyEnumerator();

            private EmptyEnumerator()
            {
            }

            public string Current => default!;

            object IEnumerator.Current => Current;

            public void Dispose()
            {
            }

            public bool MoveNext() => false;

            public void Reset()
            {
            }
        }
    }
}
