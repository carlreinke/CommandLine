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
    public static class CommandOption_1Tests
    {
        [Fact]
        public static void ParameterIsOptional_ParameterIsNotOptional_ReturnsFalse()
        {
            var rootCommand = new RootCommand("test");
            var option = rootCommand.AddOption('a', "alpha", "value", "");

            Assert.False(option.ParameterIsOptional);
        }

        [Fact]
        public static void ParameterIsOptional_ParameterIsOptional_ReturnsTrue()
        {
            var rootCommand = new RootCommand("test");
            var option = rootCommand.AddOption('a', "alpha", "value", "");
            option.SetOptionalParameterDefaultValue("default");

            Assert.True(option.ParameterIsOptional);
        }

        [Fact]
        public static void OptionalParameterDefaultValue_ParameterIsNotOptional_ThrowsInvalidOperationException()
        {
            var rootCommand = new RootCommand("test");
            var option = rootCommand.AddOption('a', "alpha", "value", "");

            var ex = Assert.Throws<InvalidOperationException>(() => option.OptionalParameterDefaultValue);

            Assert.Equal("The command option parameter is not optional.", ex.Message);
        }

        [Fact]
        public static void Accept_ParameterIsNotOptional_ThrowsInvalidOperationException()
        {
            var rootCommand = new RootCommand("test");
            var option = rootCommand.AddOption('a', "alpha", "value", "");

            var ex = Assert.Throws<InvalidOperationException>(() => option.Accept());

            Assert.Equal("The command option requires a value.", ex.Message);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        public static void Accept_ParameterIsOptional_AcceptsOptionalParameterDefaultValue(int count)
        {
            var rootCommand = new RootCommand("test");
            var option = rootCommand.AddOption('a', "alpha", "value", "");
            option.SetOptionalParameterDefaultValue("default");

            for (int i = 0; i < count; ++i)
                option.Accept();

            Assert.Equal(count, option.Count);
            Assert.Equal(option.OptionalParameterDefaultValue, option.Value);
        }

        [Fact]
        public static void TryAcceptValue_TextIsNull_ThrowsArgumentNullException()
        {
            var rootCommand = new RootCommand("test");
            var option = rootCommand.AddOption('a', "alpha", "value", "");

            var ex = Assert.Throws<ArgumentNullException>(() => option.TryAcceptValue(null!));

            Assert.Equal("text", ex.ParamName);
        }

        [Theory]
        [InlineData(new[] { "123" }, 123)]
        [InlineData(new[] { "123", "234" }, 234)]
        public static void TryAcceptValue_ValidValue_AcceptsValue(string[] texts, int expectedValue)
        {
            var rootCommand = new RootCommand("test");
            var option = rootCommand.AddOption<int>('a', "alpha", "value", "", int.TryParse);

            foreach (string text in texts)
            {
                bool result = option.TryAcceptValue(text);

                Assert.True(result);
            }

            Assert.Equal(texts.Length, option.Count);
            Assert.True(option.HasValue);
            Assert.Equal(expectedValue, option.Value);
            Assert.Equal(expectedValue, option.ValueOrDefault);
            Assert.Equal(expectedValue, option.GetValueOrDefault(-1));
            Assert.Equal(expectedValue, option.GetValueOrNull());
        }

        [Fact]
        public static void TryAcceptValue_InvalidValue_AcceptsValue()
        {
            var rootCommand = new RootCommand("test");
            var option = rootCommand.AddOption<int>('a', "alpha", "value", "", TryParser);

            bool result = option.TryAcceptValue("test");

            Assert.False(result);

            Assert.Equal(0, option.Count);
            Assert.False(option.HasValue);
            var ex = Assert.Throws<InvalidOperationException>(() => option.Value);
            Assert.Equal("The command option does not have a value.", ex.Message);
            Assert.Equal(0, option.ValueOrDefault);
            Assert.Equal(-1, option.GetValueOrDefault(-1));
            Assert.Null(option.GetValueOrNull());
        }

        [Fact]
        public static void GetValueOrNull_OptionIsNull_ThrowsArgumentNullException()
        {
            var ex = Assert.Throws<ArgumentNullException>(() => CommandOptionExtensions.GetValueOrNull<int>(null!));

            Assert.Equal("option", ex.ParamName);
        }

        private static bool TryParser(string text, [MaybeNullWhen(false)] out int value)
        {
            value = default;
            return false;
        }
    }
}
