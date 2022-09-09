// Copyright 2022 Carl Reinke
//
// This file is part of a library that is licensed under the terms of the GNU
// Lesser General Public License Version 3 as published by the Free Software
// Foundation.
//
// This license does not grant rights under trademark law for use of any trade
// names, trademarks, or service marks.

using System;
using Xunit;

namespace Tetractic.CommandLine.Tests
{
    public static class InvalidCommandLineExceptionTests
    {
        [Fact]
        public static void Constructor_CommandIsNull_ThrowsArgumentNullException()
        {
            var ex = Assert.Throws<ArgumentNullException>(() => new InvalidCommandLineException(null!, null));

            Assert.Equal("command", ex.ParamName);
        }

        [Fact]
        public static void Command_Always_ReturnsCommand()
        {
            var rootCommand = new RootCommand("test");

            var instance = new InvalidCommandLineException(rootCommand, null);

            Assert.Equal(rootCommand, instance.Command);
        }
    }
}
