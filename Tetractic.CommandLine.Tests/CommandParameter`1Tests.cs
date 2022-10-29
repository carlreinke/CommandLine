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
    public static class CommandParameter_1Tests
    {
        [Fact]
        public static void TryAcceptValue_TextIsNull_ThrowsArgumentNullException()
        {
            var rootCommand = new RootCommand("test");
            var parameter = rootCommand.AddParameter("sierra", "");

            var ex = Assert.Throws<ArgumentNullException>(() => parameter.TryAcceptValue(null!));

            Assert.Equal("text", ex.ParamName);
        }

        [Theory]
        [InlineData(new[] { "123" }, 123)]
        [InlineData(new[] { "123", "234" }, 234)]
        public static void TryAcceptValue_ValidValue_AcceptsValue(string[] texts, int expectedValue)
        {
            var rootCommand = new RootCommand("test");
            var parameter = rootCommand.AddParameter<int>("sierra", "", int.TryParse);

            foreach (string text in texts)
            {
                bool result = parameter.TryAcceptValue(text);

                Assert.True(result);
            }

            Assert.Equal(texts.Length, parameter.Count);
            Assert.True(parameter.HasValue);
            Assert.Equal(expectedValue, parameter.Value);
            Assert.Equal(expectedValue, parameter.ValueOrDefault);
            Assert.Equal(expectedValue, parameter.GetValueOrDefault(-1));
            Assert.Equal(expectedValue, parameter.GetValueOrNull());
        }

        [Fact]
        public static void TryAcceptValue_InvalidValue_AcceptsValue()
        {
            var rootCommand = new RootCommand("test");
            var parameter = rootCommand.AddParameter<int>("sierra", "", TryParser);

            bool result = parameter.TryAcceptValue("test");

            Assert.False(result);

            Assert.Equal(0, parameter.Count);
            Assert.False(parameter.HasValue);
            var ex = Assert.Throws<InvalidOperationException>(() => parameter.Value);
            Assert.Equal("The command parameter does not have a value.", ex.Message);
            Assert.Equal(0, parameter.ValueOrDefault);
            Assert.Equal(-1, parameter.GetValueOrDefault(-1));
            Assert.Null(parameter.GetValueOrNull());
        }

        [Fact]
        public static void GetValueOrNull_ParameterIsNull_ThrowsArgumentNullException()
        {
            var ex = Assert.Throws<ArgumentNullException>(() => CommandParameterExtensions.GetValueOrNull<int>(null!));

            Assert.Equal("parameter", ex.ParamName);
        }

        private static bool TryParser(string text, out int value)
        {
            value = default;
            return false;
        }
    }
}
