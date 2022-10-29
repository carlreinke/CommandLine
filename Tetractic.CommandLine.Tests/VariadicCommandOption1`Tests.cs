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
    public static class VariadicCommandOption_1Tests
    {
        [Fact]
        public static void ParameterIsOptional_ParameterIsNotOptional_ReturnsFalse()
        {
            var rootCommand = new RootCommand("test");
            var option = rootCommand.AddVariadicOption('a', "alpha", "value", "");

            Assert.False(option.ParameterIsOptional);
        }

        [Fact]
        public static void ParameterIsOptional_ParameterIsOptional_ReturnsTrue()
        {
            var rootCommand = new RootCommand("test");
            var option = rootCommand.AddVariadicOption('a', "alpha", "value", "");
            option.SetOptionalParameterDefaultValue("default");

            Assert.True(option.ParameterIsOptional);
        }

        [Fact]
        public static void OptionalParameterDefaultValue_ParameterIsNotOptional_ThrowsInvalidOperationException()
        {
            var rootCommand = new RootCommand("test");
            var option = rootCommand.AddVariadicOption('a', "alpha", "value", "");

            var ex = Assert.Throws<InvalidOperationException>(() => option.OptionalParameterDefaultValue);

            Assert.Equal("The command option parameter is not optional.", ex.Message);
        }

        [Fact]
        public static void Accept_ParameterIsNotOptional_ThrowsInvalidOperationException()
        {
            var rootCommand = new RootCommand("test");
            var option = rootCommand.AddVariadicOption('a', "alpha", "value", "");

            var ex = Assert.Throws<InvalidOperationException>(() => option.Accept());

            Assert.Equal("The command option requires a value.", ex.Message);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        public static void Accept_ParameterIsOptional_AcceptsOptionalParameterDefaultValue(int count)
        {
            var rootCommand = new RootCommand("test");
            var option = rootCommand.AddVariadicOption('a', "alpha", "value", "");
            option.SetOptionalParameterDefaultValue("default");

            for (int i = 0; i < count; ++i)
                option.Accept();

            Assert.Equal(count, option.Count);
            string[] expectedValues = new string[count];
            Array.Fill(expectedValues, option.OptionalParameterDefaultValue);
            Assert.Equal(expectedValues, option.Values);
        }

        [Fact]
        public static void TryAcceptValue_TextIsNull_ThrowsArgumentNullException()
        {
            var rootCommand = new RootCommand("test");
            var option = rootCommand.AddVariadicOption('a', "alpha", "value", "");

            var ex = Assert.Throws<ArgumentNullException>(() => option.TryAcceptValue(null!));

            Assert.Equal("text", ex.ParamName);
        }

        [Theory]
        [InlineData(new[] { "123" }, new[] { 123 })]
        [InlineData(new[] { "123", "234" }, new[] { 123, 234 })]
        public static void TryAcceptValue_ValidValue_AcceptsValue(string[] texts, int[] expectedValues)
        {
            var rootCommand = new RootCommand("test");
            var option = rootCommand.AddVariadicOption<int>('a', "alpha", "value", "", int.TryParse);

            foreach (string text in texts)
            {
                bool result = option.TryAcceptValue(text);

                Assert.True(result);
            }

            Assert.Equal(texts.Length, option.Count);
            Assert.Equal(expectedValues, option.Values);
        }

        [Fact]
        public static void TryAcceptValue_InvalidValue_AcceptsValue()
        {
            var rootCommand = new RootCommand("test");
            var option = rootCommand.AddVariadicOption<int>('a', "alpha", "value", "", TryParser);

            bool result = option.TryAcceptValue("test");

            Assert.False(result);

            Assert.Equal(0, option.Count);
            Assert.Empty(option.Values);
        }

        private static bool TryParser(string text, out int value)
        {
            value = default;
            return false;
        }
    }
}
