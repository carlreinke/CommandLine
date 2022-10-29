// Copyright 2022 Carl Reinke
//
// This file is part of a library that is licensed under the terms of the GNU
// Lesser General Public License Version 3 as published by the Free Software
// Foundation.
//
// This license does not grant rights under trademark law for use of any trade
// names, trademarks, or service marks.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;

namespace Tetractic.CommandLine.Tests
{
    public static class WindowsWildcardExpanderTests
    {
        [Theory]
        [InlineData("*", true)]
        [InlineData("?", true)]
        [InlineData("x", false)]
        public static void ContainsAnyWildcard_Valid_ReturnsExpectedResult(string pattern, bool expectedResult)
        {
            bool result = WindowsWildcardExpander.ContainsAnyWildcard(pattern);

            Assert.Equal(expectedResult, result);
        }

        [Theory]
        [InlineData("", new[]  // Empty
        {
            "",
        })]
        [InlineData("\0", new[]  // Invalid path character
        {
            "\0",
        })]
        [InlineData("*", new[]  // Asterisk
        {
            "Test",
            "Tes_",
            "Te_t",
            "T_st",
            "_est",
        })]
        [InlineData("**", new[]
        {
            "Test",
            "Tes_",
            "Te_t",
            "T_st",
            "_est",
        })]
        [InlineData("*_*", new[]
        {
            "Tes_",
            "Te_t",
            "T_st",
            "_est",
        })]
        [InlineData("*Test", new[]
        {
            "Test",
        })]
        [InlineData("*est", new[]
        {
            "Test",
            "_est",
        })]
        [InlineData("*st", new[]
        {
            "Test",
            "T_st",
            "_est",
        })]
        [InlineData("Te*st", new[]
        {
            "Test",
        })]
        [InlineData("T*st", new[]
        {
            "Test",
            "T_st",
        })]
        [InlineData("T*t", new[]
        {
            "Test",
            "Te_t",
            "T_st",
        })]
        [InlineData("Test*", new[]
        {
            "Test",
        })]
        [InlineData("Tes*", new[]
        {
            "Test",
            "Tes_",
        })]
        [InlineData("Te*", new[]
        {
            "Test",
            "Tes_",
            "Te_t",
        })]
        [InlineData("?est", new[]  // Question mark
        {
            "Test",
            "_est",
        })]
        [InlineData("T?st", new[]
        {
            "Test",
            "T_st",
        })]
        [InlineData("Tes?", new[]
        {
            "Test",
            "Tes_",
        })]
        [InlineData("Te??", new[]
        {
            "Test",
            "Tes_",
            "Te_t",
        })]
        [InlineData("Te?", new[]  // Pattern shorter than name
        {
            "Te?",
        })]
        [InlineData("Test?", new[]  // Pattern longer than name
        {
            "Test?",
        })]
        [InlineData("Tes_›*", new[]  // Pattern has no child to match against
        {
            "Tes_›*",
        })]
        [InlineData("Test_›*", new[]  // Pattern has no parent to match within
        {
            "Test_›*",
        })]
        [InlineData("*›Test1", new[]  // Literal after pattern
        {
            "Te_t›Test1",
            "T_st›Test1",
        })]
        [InlineData("*>Test1", new[]  // Literal after pattern (alternate separator)
        {
            "Te_t>Test1",
            "T_st>Test1",
        })]
        [InlineData("T_st›*1", new[]  // Pattern after literal
        {
            "T_st›Test1",
        })]
        [InlineData("T_st>*1", new[]  // Pattern after literal (alternate separator)
        {
            "T_st>Test1",
        })]
        [InlineData("*›", new[]  // Separator after pattern -- returns only directories
        {
            "Tes_›",
            "Te_t›",
            "T_st›",
        })]
        [InlineData("t_sT›T?st1", new[]  // Different-case literal -- returned case matches pattern
        {
            "t_sT›Test1",
        })]
        [InlineData("T_st›t?sT1", new[]  // Different-case pattern -- returned case matches source
        {
            "T_st›Test1",
        })]
        public static void EnumerateMatches_Always_ReturnsExpectedResults(string pattern, string[] expectedResults)
        {
            string[] results = WindowsWildcardExpander<Provider>.EnumerateMatches(pattern).ToArray();

            Assert.Equal(expectedResults, results);
        }

        private readonly struct Provider : WindowsWildcardExpander.IDirectoryProvider, WindowsWildcardExpander.IFileProvider, WindowsWildcardExpander.IPathProvider
        {
            private static readonly string[] _paths = new[]
            {
                "X»›",
                "X»›T_st›",
                "X»›T_st›Test1",
                "X»›T_st›Test2",
                "X»›Te_t›",
                "X»›Te_t›Test1›",
                "X»›Te_t›Test2›",
                "X»›Tes_›",
                "X»›Test",
                "X»›_est",
            };

            private static readonly string _workingDirectory = "X»›";

            private static readonly Path _path = new Path(_workingDirectory);

            private static readonly FileProvider _fileProvider = new FileProvider(_path, _paths);

            private static readonly DirectoryProvider _directoryProvider = new DirectoryProvider(_path, _paths);

            char WindowsWildcardExpander.IPathProvider.AltDirectorySeparatorChar => Path.AltDirectorySeparatorChar;

            char WindowsWildcardExpander.IPathProvider.DirectorySeparatorChar => Path.DirectorySeparatorChar;

            char[] WindowsWildcardExpander.IPathProvider.InvalidPathChars => Path.InvalidPathChars;

            char WindowsWildcardExpander.IPathProvider.VolumeSeparatorChar => Path.VolumeSeparatorChar;

            IEnumerable<string> WindowsWildcardExpander.IDirectoryProvider.EnumerateDirectories(string path) => _directoryProvider.EnumerateDirectories(path);

            IEnumerable<string> WindowsWildcardExpander.IDirectoryProvider.EnumerateFileSystemEntries(string path) => _directoryProvider.EnumerateFileSystemEntries(path);

            bool WindowsWildcardExpander.IDirectoryProvider.Exists(string path) => _directoryProvider.Exists(path);

            bool WindowsWildcardExpander.IFileProvider.Exists(string path) => _fileProvider.Exists(path);

            string WindowsWildcardExpander.IPathProvider.GetFileName(string path) => Path.GetFileName(path);
        }

        [Theory]
        [InlineData("*", new[]
        {
            "Test1",
        })]
        [InlineData("X»*", new[]
        {
            "X»Test1",
        })]
        [InlineData("X»›*", new[]
        {
            "X»›Test",
            "X»›Tes_",
            "X»›Te_t",
        })]
        public static void EnumerateMatches_VolumeAndVolumeRoot_ReturnsExpectedResults(string pattern, string[] expectedResults)
        {
            string[] results = WindowsWildcardExpander<VolumeAndRootProvider>.EnumerateMatches(pattern).ToArray();

            Assert.Equal(expectedResults, results);
        }

        private readonly struct VolumeAndRootProvider : WindowsWildcardExpander.IDirectoryProvider, WindowsWildcardExpander.IFileProvider, WindowsWildcardExpander.IPathProvider
        {
            private static readonly string[] _paths = new[]
            {
                "X»›",
                "X»›Te_t›",
                "X»›Te_t›Test1",
                "X»›Tes_›",
                "X»›Tes_›Test2",
                "X»›Test",
            };

            private static readonly string _workingDirectory = "X»›Te_t›";

            private static readonly Path _path = new Path(_workingDirectory);

            private static readonly FileProvider _fileProvider = new FileProvider(_path, _paths);

            private static readonly DirectoryProvider _directoryProvider = new DirectoryProvider(_path, _paths);

            char WindowsWildcardExpander.IPathProvider.AltDirectorySeparatorChar => Path.AltDirectorySeparatorChar;

            char WindowsWildcardExpander.IPathProvider.DirectorySeparatorChar => Path.DirectorySeparatorChar;

            char[] WindowsWildcardExpander.IPathProvider.InvalidPathChars => Path.InvalidPathChars;

            char WindowsWildcardExpander.IPathProvider.VolumeSeparatorChar => Path.VolumeSeparatorChar;

            IEnumerable<string> WindowsWildcardExpander.IDirectoryProvider.EnumerateDirectories(string path) => _directoryProvider.EnumerateDirectories(path);

            IEnumerable<string> WindowsWildcardExpander.IDirectoryProvider.EnumerateFileSystemEntries(string path) => _directoryProvider.EnumerateFileSystemEntries(path);

            bool WindowsWildcardExpander.IDirectoryProvider.Exists(string path) => _directoryProvider.Exists(path);

            bool WindowsWildcardExpander.IFileProvider.Exists(string path) => _fileProvider.Exists(path);

            string WindowsWildcardExpander.IPathProvider.GetFileName(string path) => Path.GetFileName(path);
        }

        [Theory]
        [InlineData("*›*", new[]
        {
            "A›Test1",
            "Z›Test3",
        })]
        public static void EnumerateMatches_DirectoryEnumerateThrows_ReturnsExpectedResults(string pattern, string[] expectedResults)
        {
            string[] results = WindowsWildcardExpander<DirectoryEnumerateThrowsProvider>.EnumerateMatches(pattern).ToArray();

            Assert.Equal(expectedResults, results);
        }

        private readonly struct DirectoryEnumerateThrowsProvider : WindowsWildcardExpander.IDirectoryProvider, WindowsWildcardExpander.IFileProvider, WindowsWildcardExpander.IPathProvider
        {
            private static readonly string[] _paths = new[]
            {
                "X»›",
                "X»›A›",
                "X»›A›Test1",
                "X»›F›",
                "X»›F›Test2",
                "X»›Z›",
                "X»›Z›Test3",
            };

            private static readonly string _workingDirectory = "X»›";

            private static readonly Path _path = new Path(_workingDirectory);

            private static readonly FileProvider _fileProvider = new FileProvider(_path, _paths);

            private static readonly DirectoryProvider _directoryProvider = new DirectoryProvider(_path, _paths);

            char WindowsWildcardExpander.IPathProvider.AltDirectorySeparatorChar => Path.AltDirectorySeparatorChar;

            char WindowsWildcardExpander.IPathProvider.DirectorySeparatorChar => Path.DirectorySeparatorChar;

            char[] WindowsWildcardExpander.IPathProvider.InvalidPathChars => Path.InvalidPathChars;

            char WindowsWildcardExpander.IPathProvider.VolumeSeparatorChar => Path.VolumeSeparatorChar;

            IEnumerable<string> WindowsWildcardExpander.IDirectoryProvider.EnumerateDirectories(string path)
            {
                if (path.Length == 0)
                    throw new ArgumentException("Invalid.", nameof(path));
                if (path.IndexOfAny(Path.InvalidPathChars) >= 0)
                    throw new ArgumentException("Invalid.", nameof(path));

                if (!path.EndsWith(Path.DirectorySeparatorChar) && !path.EndsWith(Path.AltDirectorySeparatorChar))
                    path += Path.DirectorySeparatorChar;

                path = _path.GetFullPath(path);

                ThrowIfUnauthorized(path);

                return _directoryProvider.EnumerateDirectories(path);
            }

            IEnumerable<string> WindowsWildcardExpander.IDirectoryProvider.EnumerateFileSystemEntries(string path)
            {
                if (path.Length == 0)
                    throw new ArgumentException("Invalid.", nameof(path));
                if (path.IndexOfAny(Path.InvalidPathChars) >= 0)
                    throw new ArgumentException("Invalid.", nameof(path));

                if (!path.EndsWith(Path.DirectorySeparatorChar) && !path.EndsWith(Path.AltDirectorySeparatorChar))
                    path += Path.DirectorySeparatorChar;

                path = _path.GetFullPath(path);

                ThrowIfUnauthorized(path);

                return _directoryProvider.EnumerateFileSystemEntries(path);
            }

            bool WindowsWildcardExpander.IDirectoryProvider.Exists(string path) => _directoryProvider.Exists(path);

            bool WindowsWildcardExpander.IFileProvider.Exists(string path) => _fileProvider.Exists(path);

            string WindowsWildcardExpander.IPathProvider.GetFileName(string path) => Path.GetFileName(path);

            private static void ThrowIfUnauthorized(string path)
            {
                if (path.Equals("X»›F", StringComparison.OrdinalIgnoreCase) ||
                    path.StartsWith("X»›F›", StringComparison.OrdinalIgnoreCase))
                {
                    throw new UnauthorizedAccessException("Unauthorized.");
                }
            }
        }

        private sealed class Path
        {
            public static readonly char AltDirectorySeparatorChar = '>';

            public static readonly char DirectorySeparatorChar = '›';

            public static readonly char[] InvalidPathChars = new[] { '\0', '*', '?' };

            public static readonly char VolumeSeparatorChar = '»';

            private readonly string _workingDirectory;

            public Path(string workingDirectory)
            {
                _workingDirectory = workingDirectory;
            }

            public static string GetFileName(string path)
            {
                int index = path.LastIndexOfAny(new[] { DirectorySeparatorChar, AltDirectorySeparatorChar, VolumeSeparatorChar });

                return path.Substring(index + 1);
            }

            public string GetFullPath(string path)
            {
                int vsi = path.IndexOf(VolumeSeparatorChar);
                if (vsi >= 0)
                {
                    if (vsi == path.Length - 1)
                        path = _workingDirectory;
                    else if (!IsAnyDirectorySeparator(path[vsi + 1]))
                        path = Combine(_workingDirectory, path.Substring(vsi + 1));

                    return NormalizePath(path);
                }

                path = Combine(_workingDirectory, path);

                return NormalizePath(path);
            }

            public static string Combine(string path1, string path2)
            {
                if (path2.Length == 0)
                    return path1;
                if (path1.Length == 0 || IsAnyDirectorySeparator(path2[0]) || path2.EndsWith(VolumeSeparatorChar))
                    return path2;
                return IsAnySeparator(path1[path1.Length - 1])
                    ? path1 + path2
                    : path1 + DirectorySeparatorChar + path2;
            }

            internal static string NormalizePath(string path)
            {
                var segments = path.Split(new char[] { DirectorySeparatorChar, AltDirectorySeparatorChar }).ToList();
                int dotDots = 0;
                for (int i = segments.Count - 1; i >= 0; --i)
                {
                    string volume = string.Empty;
                    string segment = segments[i];
                    if (i == 0)
                    {
                        int vsi = segment.IndexOf(VolumeSeparatorChar);
                        if (vsi >= 0)
                        {
                            volume = segment.Substring(0, vsi + 1);
                            segment = segment.Substring(vsi + 1);
                            segments[i] = segment;
                        }
                    }
                    if (segment.Length == 0)
                    {
                        if (i != 0 && i != segments.Count - 1)
                            segments.RemoveAt(i);
                    }
                    else if (segment == ".")
                    {
                        segments.RemoveAt(i);
                    }
                    else if (segment == "..")
                    {
                        dotDots += 1;
                    }
                    else if (dotDots > 0)
                    {
                        segments.RemoveAt(i + 1);
                        segments.RemoveAt(i);
                        dotDots -= 1;
                    }
                    if (i == 0)
                        segments[i] = volume + segments[i];
                }
                return string.Join(DirectorySeparatorChar, segments);
            }

            internal static bool EndsWithAnySeparator(string path)
            {
                return path.Length > 0 && IsAnySeparator(path[path.Length - 1]);
            }

            internal static bool IsAnySeparator(char c)
            {
                return c == DirectorySeparatorChar || c == AltDirectorySeparatorChar || c == VolumeSeparatorChar;
            }

            internal static bool EndsWithAnyDirectorySeparator(string path)
            {
                return path.Length > 0 && IsAnyDirectorySeparator(path[path.Length - 1]);
            }

            internal static bool IsAnyDirectorySeparator(char c)
            {
                return c == DirectorySeparatorChar || c == AltDirectorySeparatorChar;
            }
        }

        private sealed class DirectoryProvider : WindowsWildcardExpander.IDirectoryProvider
        {
            private readonly Path _path;

            private readonly string[] _paths;

            public DirectoryProvider(Path path, string[] paths)
            {
                _path = path;
                _paths = paths;
            }

            public IEnumerable<string> EnumerateDirectories(string path)
            {
                if (path.Length == 0)
                    throw new ArgumentException("Invalid.", nameof(path));
                if (path.IndexOfAny(Path.InvalidPathChars) >= 0)
                    throw new ArgumentException("Invalid.", nameof(path));

                return EnumerateFileSystemEntries(path, includeFiles: false);
            }

            public IEnumerable<string> EnumerateFileSystemEntries(string path)
            {
                if (path.Length == 0)
                    throw new ArgumentException("Invalid.", nameof(path));
                if (path.IndexOfAny(Path.InvalidPathChars) >= 0)
                    throw new ArgumentException("Invalid.", nameof(path));

                return EnumerateFileSystemEntries(path, includeFiles: true);
            }

            public bool Exists(string path)
            {
                if (path.Length == 0)
                    return false;
                if (path.IndexOfAny(Path.InvalidPathChars) >= 0)
                    return false;

                path = _path.GetFullPath(path);

                if (!path.EndsWith(Path.DirectorySeparatorChar))
                    path += Path.DirectorySeparatorChar;

                return _paths.Contains(path, StringComparer.OrdinalIgnoreCase);
            }

            internal IEnumerable<string> EnumerateFileSystemEntries(string path, bool includeFiles)
            {
                if (!Path.EndsWithAnySeparator(path))
                    path += Path.DirectorySeparatorChar;

                string originalPath = path;

                path = _path.GetFullPath(path);

                if (!_paths.Contains(path, StringComparer.OrdinalIgnoreCase))
                    throw new DirectoryNotFoundException();

                foreach (string x in _paths)
                {
                    if (x.StartsWith(path, StringComparison.OrdinalIgnoreCase) &&
                        !x.Equals(path, StringComparison.OrdinalIgnoreCase))
                    {
                        int index = x.IndexOf(Path.DirectorySeparatorChar, path.Length);
                        if (index < 0)
                        {
                            if (includeFiles)
                                yield return string.Concat(originalPath, x.AsSpan(path.Length));
                        }
                        else if (index == x.Length - 1)
                        {
                            yield return string.Concat(originalPath, x.AsSpan(path.Length, x.Length - path.Length - 1));
                        }
                    }
                }
            }
        }

        private sealed class FileProvider : WindowsWildcardExpander.IFileProvider
        {
            private readonly Path _path;

            private readonly string[] _paths;

            public FileProvider(Path path, string[] paths)
            {
                _path = path;
                _paths = paths;
            }

            public bool Exists(string path)
            {
                if (path.Length == 0)
                    return false;
                if (path.IndexOfAny(Path.InvalidPathChars) >= 0)
                    return false;

                path = _path.GetFullPath(path);

                if (path.EndsWith(Path.DirectorySeparatorChar))
                    return false;

                return _paths.Contains(path, StringComparer.OrdinalIgnoreCase);
            }
        }
    }
}
