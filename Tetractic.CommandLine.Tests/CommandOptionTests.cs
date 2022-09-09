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
    public static class CommandOptionTests
    {
        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        public static void Accept_Always_IncrementsCount(int count)
        {
            var rootCommand = new RootCommand("test");
            var option = rootCommand.AddOption('a', "alpha", "");

            for (int i = 0; i < count; ++i)
                option.Accept();

            Assert.Equal(count, option.Count);
        }

        [Fact]
        public static void TryAcceptValue_TextIsNull_ThrowsArgumentNullException()
        {
            var rootCommand = new RootCommand("test");
            var option = rootCommand.AddOption('a', "alpha", "");

            var ex = Assert.Throws<ArgumentNullException>(() => option.TryAcceptValue(null!));

            Assert.Equal("text", ex.ParamName);
        }

        [Fact]
        public static void TryAcceptValue_Otherwise_ThrowsInvalidOperationException()
        {
            var rootCommand = new RootCommand("test");
            var option = rootCommand.AddOption('a', "alpha", "");

            var ex = Assert.Throws<InvalidOperationException>(() => option.TryAcceptValue(""));

            Assert.Equal("The command option does not expect a value.", ex.Message);
        }
    }
}
