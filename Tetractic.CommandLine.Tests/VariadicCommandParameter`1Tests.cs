// Copyright 2022 Carl Reinke
//
// This file is part of a library that is licensed under the terms of GNU Lesser
// General Public License version 3 as published by the Free Software
// Foundation.
//
// This license does not grant rights under trademark law for use of any trade
// names, trademarks, or service marks.

using System;
using System.Diagnostics.CodeAnalysis;
using Xunit;

namespace Tetractic.CommandLine.Tests
{
    public static class VariadicCommandParameter_1Tests
    {
        [Fact]
        public static void TryAcceptValue_TextIsNull_ThrowsArgumentNullException()
        {
            var rootCommand = new RootCommand("test");
            var parameter = rootCommand.AddVariadicParameter("sierra", "");

            var ex = Assert.Throws<ArgumentNullException>(() => parameter.TryAcceptValue(null!));

            Assert.Equal("text", ex.ParamName);
        }

        [Theory]
        [InlineData(new[] { "123" }, new[] { 123 })]
        [InlineData(new[] { "123", "234" }, new[] { 123, 234 })]
        public static void TryAcceptValue_ValidValue_AcceptsValue(string[] texts, int[] expectedValues)
        {
            var rootCommand = new RootCommand("test");
            var parameter = rootCommand.AddVariadicParameter<int>("sierra", "", int.TryParse);

            foreach (string text in texts)
            {
                bool result = parameter.TryAcceptValue(text);

                Assert.True(result);
            }

            Assert.Equal(texts.Length, parameter.Count);
            Assert.Equal(expectedValues, parameter.Values);
        }

        [Fact]
        public static void TryAcceptValue_InvalidValue_AcceptsValue()
        {
            var rootCommand = new RootCommand("test");
            var parameter = rootCommand.AddVariadicParameter<int>("sierra", "", TryParser);

            bool result = parameter.TryAcceptValue("test");

            Assert.False(result);

            Assert.Equal(0, parameter.Count);
            Assert.Empty(parameter.Values);
        }

        private static bool TryParser(string text, [MaybeNullWhen(false)] out int value)
        {
            value = default;
            return false;
        }
    }
}
