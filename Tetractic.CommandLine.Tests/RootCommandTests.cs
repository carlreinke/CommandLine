// Copyright 2024 Carl Reinke
//
// This file is part of a library that is licensed under the terms of the GNU
// Lesser General Public License Version 3 as published by the Free Software
// Foundation.
//
// This license does not grant rights under trademark law for use of any trade
// names, trademarks, or service marks.

using System;
using System.IO;
using System.Runtime.InteropServices;
using Xunit;

namespace Tetractic.CommandLine.Tests
{
    public static class RootCommandTests
    {
        [Fact]
        public static void Create_NameIsNull_ThrowsArgumentNullException()
        {
            var ex = Assert.Throws<ArgumentNullException>(() => new RootCommand(null!));

            Assert.Equal("name", ex.ParamName);
        }

        [Fact]
        public static void Create_Configure_IsInvoked()
        {
            var rootCommand = new RootCommand("test");
            rootCommand.HelpVisibility = HelpVisibility.Never;

            Assert.Equal(HelpVisibility.Never, rootCommand.HelpVisibility);
        }

        [Fact]
        public static void Execute_ArgsIsNull_ThrowsArgumentNullException()
        {
            var rootCommand = new RootCommand("test");

            SetThrowingHelpHandlerRecursive(rootCommand);

            var ex = Assert.Throws<ArgumentNullException>(() => rootCommand.Execute(null!));

            Assert.Equal("args", ex.ParamName);
        }

        [Fact]
        public static void Execute_ArgsElementIsNull_ThrowsArgumentNullException()
        {
            var rootCommand = new RootCommand("test");

            SetThrowingHelpHandlerRecursive(rootCommand);

            var ex = Assert.Throws<ArgumentNullException>(() => rootCommand.Execute(new string[] { null! }));

            Assert.Equal("args", ex.ParamName);
        }

        [Fact]
        public static void Execute_DashArgument_ArgumentIsStoredInParameter()
        {
            var rootCommand = new RootCommand("test");
            {
                var parameter = rootCommand.AddParameter("test", "");

                rootCommand.SetInvokeHandler(() =>
                {
                    Assert.Equal("-", parameter.ValueOrDefault);

                    return 123;
                });
            }

            SetThrowingHelpHandlerRecursive(rootCommand);

            int returnCode = rootCommand.Execute(new[] { "-" });

            Assert.Equal(123, returnCode);
        }

        [Theory]
        [InlineData(new[] { "-a" }, "-a")]
        [InlineData(new[] { "--alpha" }, "--alpha")]
        public static void Execute_NonexistentOption_ThrowsInvalidCommandLineException(string[] args, string optionName)
        {
            var rootCommand = new RootCommand("test");

            SetThrowingHelpHandlerRecursive(rootCommand);

            var ex = Assert.Throws<InvalidCommandLineException>(() => rootCommand.Execute(args));

            Assert.Same(rootCommand, ex.Command);
            Assert.Equal($@"Unrecognized option ""{optionName}"".", ex.Message);
        }

        [Theory]
        [InlineData(new[] { "-a" }, 1, 0)]
        [InlineData(new[] { "-aa" }, 2, 0)]
        [InlineData(new[] { "-ab" }, 1, 1)]
        [InlineData(new[] { "-ba" }, 1, 1)]
        [InlineData(new[] { "-a", "-a" }, 2, 0)]
        [InlineData(new[] { "-a", "-b" }, 1, 1)]
        [InlineData(new[] { "-b", "-a" }, 1, 1)]
        [InlineData(new[] { "--alpha" }, 1, 0)]
        [InlineData(new[] { "--alpha", "--alpha" }, 2, 0)]
        [InlineData(new[] { "--alpha", "--bravo" }, 1, 1)]
        [InlineData(new[] { "--bravo", "--alpha" }, 1, 1)]
        public static void Execute_UnparameterizedOption_OptionCountIsIncremented(string[] args, int expectedOptionACount, int expectedOptionBCount)
        {
            var rootCommand = new RootCommand("test");
            {
                var optionA = rootCommand.AddOption('a', "alpha", "");
                var optionB = rootCommand.AddOption('b', "bravo", "");

                rootCommand.SetInvokeHandler(() =>
                {
                    Assert.Equal(expectedOptionACount, optionA.Count);
                    Assert.Equal(expectedOptionBCount, optionB.Count);

                    return 123;
                });
            }

            SetThrowingHelpHandlerRecursive(rootCommand);

            int returnCode = rootCommand.Execute(args);

            Assert.Equal(123, returnCode);
        }

        [Theory]
        [InlineData(new[] { "-a=test" }, "-a", "test")]
        [InlineData(new[] { "--alpha=test" }, "--alpha", "test")]
        public static void Execute_UnparameterizedOptionUnexpectedValue_ThrowsInvalidCommandLineException(string[] args, string optionName, string valueText)
        {
            var rootCommand = new RootCommand("test");
            {
                _ = rootCommand.AddOption('a', "alpha", "");
            }

            SetThrowingHelpHandlerRecursive(rootCommand);

            var ex = Assert.Throws<InvalidCommandLineException>(() => rootCommand.Execute(args));

            Assert.Equal($@"Unexpected value ""{valueText}"" for option ""{optionName}"".", ex.Message);
            Assert.Equal(rootCommand, ex.Command);
        }

        [Theory]
        [InlineData(new[] { "-a" }, "-a")]
        [InlineData(new[] { "-ab" }, "-a")]
        [InlineData(new[] { "--alpha" }, "--alpha")]
        public static void Execute_ParameterizedOptionMissingValue_ThrowsInvalidCommandLineException(string[] args, string optionName)
        {
            var rootCommand = new RootCommand("test");
            {
                _ = rootCommand.AddOption<int>('a', "alpha", "value", "", int.TryParse);
                _ = rootCommand.AddOption('b', null, "");
            }

            SetThrowingHelpHandlerRecursive(rootCommand);

            var ex = Assert.Throws<InvalidCommandLineException>(() => rootCommand.Execute(args));

            Assert.Equal(rootCommand, ex.Command);
            Assert.Equal($@"Missing value for option ""{optionName}"".", ex.Message);
        }

        [Theory]
        [InlineData(new[] { "-a", "test" }, 1, "test", 0)]
        [InlineData(new[] { "-ba", "test" }, 1, "test", 1)]
        [InlineData(new[] { "-a=test" }, 1, "test", 0)]
        [InlineData(new[] { "-ba=test" }, 1, "test", 1)]
        [InlineData(new[] { "-a", "test", "-a", "again" }, 2, "again", 0)]
        [InlineData(new[] { "--alpha", "test" }, 1, "test", 0)]
        [InlineData(new[] { "--alpha=test" }, 1, "test", 0)]
        [InlineData(new[] { "--alpha", "test", "--alpha", "again" }, 2, "again", 0)]
        public static void Execute_ParameterizedOption_ArgumentIsStoredInParameter(string[] args, int expectedOptionACount, string expectedOptionAValue, int expectedOptionBCount)
        {
            var rootCommand = new RootCommand("test");
            {
                var optionA = rootCommand.AddOption('a', "alpha", "value", "");
                var optionB = rootCommand.AddOption('b', null, "");

                rootCommand.SetInvokeHandler(() =>
                {
                    Assert.Equal(expectedOptionACount, optionA.Count);
                    Assert.Equal(expectedOptionAValue, optionA.Value);
                    Assert.Equal(expectedOptionBCount, optionB.Count);

                    return 123;
                });
            }

            SetThrowingHelpHandlerRecursive(rootCommand);

            int returnCode = rootCommand.Execute(args);

            Assert.Equal(123, returnCode);
        }

        [Theory]
        [InlineData(new[] { "-a", "test" }, 1, new[] { "test" }, 0)]
        [InlineData(new[] { "-ba", "test" }, 1, new[] { "test" }, 1)]
        [InlineData(new[] { "-a=test" }, 1, new[] { "test" }, 0)]
        [InlineData(new[] { "-ba=test" }, 1, new[] { "test" }, 1)]
        [InlineData(new[] { "-a", "test", "-a", "again" }, 2, new[] { "test", "again" }, 0)]
        [InlineData(new[] { "--alpha", "test" }, 1, new[] { "test" }, 0)]
        [InlineData(new[] { "--alpha=test" }, 1, new[] { "test" }, 0)]
        [InlineData(new[] { "--alpha", "test", "--alpha", "again" }, 2, new[] { "test", "again" }, 0)]
        public static void Execute_VariadicParameterizedOption_ArgumentIsStoredInParameter(string[] args, int expectedOptionACount, string[] expectedOptionAValues, int expectedOptionBCount)
        {
            var rootCommand = new RootCommand("test");
            {
                var optionA = rootCommand.AddVariadicOption('a', "alpha", "value", "");
                var optionB = rootCommand.AddOption('b', null, "");

                rootCommand.SetInvokeHandler(() =>
                {
                    Assert.Equal(expectedOptionACount, optionA.Count);
                    Assert.Equal(expectedOptionAValues, optionA.Values);
                    Assert.Equal(expectedOptionBCount, optionB.Count);

                    return 123;
                });
            }

            SetThrowingHelpHandlerRecursive(rootCommand);

            int returnCode = rootCommand.Execute(args);

            Assert.Equal(123, returnCode);
        }

        [Theory]
        [InlineData(new[] { "-a" }, 1, "none", 0)]
        [InlineData(new[] { "-ab" }, 1, "none", 1)]
        [InlineData(new[] { "-a=test" }, 1, "test", 0)]
        [InlineData(new[] { "-ba=test" }, 1, "test", 1)]
        [InlineData(new[] { "-a=test", "-a=again" }, 2, "again", 0)]
        [InlineData(new[] { "-a", "-a=again" }, 2, "again", 0)]
        [InlineData(new[] { "-aa=again" }, 2, "again", 0)]
        [InlineData(new[] { "-a=test", "-a" }, 2, "none", 0)]
        [InlineData(new[] { "--alpha" }, 1, "none", 0)]
        [InlineData(new[] { "--alpha=test" }, 1, "test", 0)]
        [InlineData(new[] { "--alpha=test", "--alpha=again" }, 2, "again", 0)]
        [InlineData(new[] { "--alpha", "--alpha=again" }, 2, "again", 0)]
        [InlineData(new[] { "--alpha=test", "--alpha" }, 2, "none", 0)]
        public static void Execute_ParameterizedOptionWithOptionalParameter_ArgumentOrDefaultIsStoredInParameter(string[] args, int expectedOptionACount, string expectedOptionAValue, int expectedOptionBCount)
        {
            var rootCommand = new RootCommand("test");
            {
                var optionA = rootCommand.AddOption('a', "alpha", "value", "");
                optionA.SetOptionalParameterDefaultValue("none");
                var optionB = rootCommand.AddOption('b', null, "");

                rootCommand.SetInvokeHandler(() =>
                {
                    Assert.Equal(expectedOptionACount, optionA.Count);
                    Assert.Equal(expectedOptionAValue, optionA.Value);
                    Assert.Equal(expectedOptionBCount, optionB.Count);

                    return 123;
                });
            }

            SetThrowingHelpHandlerRecursive(rootCommand);

            int returnCode = rootCommand.Execute(args);

            Assert.Equal(123, returnCode);
        }

        [Theory]
        [InlineData(new[] { "-a" }, 1, new[] { "none" }, 0)]
        [InlineData(new[] { "-ab" }, 1, new[] { "none" }, 1)]
        [InlineData(new[] { "-a=test" }, 1, new[] { "test" }, 0)]
        [InlineData(new[] { "-ba=test" }, 1, new[] { "test" }, 1)]
        [InlineData(new[] { "-a=test", "-a=again" }, 2, new[] { "test", "again" }, 0)]
        [InlineData(new[] { "-a", "-a=again" }, 2, new[] { "none", "again" }, 0)]
        [InlineData(new[] { "-aa=again" }, 2, new[] { "none", "again" }, 0)]
        [InlineData(new[] { "-a=test", "-a" }, 2, new[] { "test", "none" }, 0)]
        [InlineData(new[] { "--alpha" }, 1, new[] { "none" }, 0)]
        [InlineData(new[] { "--alpha=test" }, 1, new[] { "test" }, 0)]
        [InlineData(new[] { "--alpha=test", "--alpha=again" }, 2, new[] { "test", "again" }, 0)]
        [InlineData(new[] { "--alpha", "--alpha=again" }, 2, new[] { "none", "again" }, 0)]
        [InlineData(new[] { "--alpha=test", "--alpha" }, 2, new[] { "test", "none" }, 0)]
        public static void Execute_VariadicParameterizedOptionWithOptionalParameter_ArgumentOrDefaultIsStoredInParameter(string[] args, int expectedOptionACount, string[] expectedOptionAValue, int expectedOptionBCount)
        {
            var rootCommand = new RootCommand("test");
            {
                var optionA = rootCommand.AddVariadicOption('a', "alpha", "value", "");
                optionA.SetOptionalParameterDefaultValue("none");
                var optionB = rootCommand.AddOption('b', null, "");

                rootCommand.SetInvokeHandler(() =>
                {
                    Assert.Equal(expectedOptionACount, optionA.Count);
                    Assert.Equal(expectedOptionAValue, optionA.Values);
                    Assert.Equal(expectedOptionBCount, optionB.Count);

                    return 123;
                });
            }

            SetThrowingHelpHandlerRecursive(rootCommand);

            int returnCode = rootCommand.Execute(args);

            Assert.Equal(123, returnCode);
        }

        [Theory]
        [InlineData(new[] { "-a", "test" }, "-a", "test")]
        [InlineData(new[] { "-a=test" }, "-a", "test")]
        [InlineData(new[] { "--alpha", "test" }, "--alpha", "test")]
        [InlineData(new[] { "--alpha=test" }, "--alpha", "test")]
        public static void Execute_OptionInvalidValue_ThrowsInvalidCommandLineException(string[] args, string optionName, string valueText)
        {
            var rootCommand = new RootCommand("test");
            {
                _ = rootCommand.AddOption<int>('a', "alpha", "value", "", int.TryParse);
            }

            SetThrowingHelpHandlerRecursive(rootCommand);

            var ex = Assert.Throws<InvalidCommandLineException>(() => rootCommand.Execute(args));

            Assert.Equal($@"Invalid value ""{valueText}"" for option ""{optionName}"".", ex.Message);
            Assert.Equal(rootCommand, ex.Command);
        }

        [Theory]
        [InlineData(new[] { "-b", "delta" }, 0, 1, 0)]
        [InlineData(new[] { "delta", "-b" }, 0, 1, 0)]
        [InlineData(new[] { "delta", "-c" }, 0, 0, 1)]
        public static void Execute_OptionInValidPosition_OptionCountIsIncremented(string[] args, int expectedOptionACount, int expectedOptionBCount, int expectedOptionCCount)
        {
            var rootCommand = new RootCommand("test");
            {
                var optionA = rootCommand.AddOption('a', null, "");
                var optionB = rootCommand.AddOption('b', null, "", inherited: true);

                var command = rootCommand.AddSubcommand("delta", "");
                {
                    var optionC = command.AddOption('c', null, "");

                    command.SetInvokeHandler(() =>
                    {
                        Assert.Equal(expectedOptionACount, optionA.Count);
                        Assert.Equal(expectedOptionBCount, optionB.Count);
                        Assert.Equal(expectedOptionCCount, optionC.Count);

                        return 123;
                    });
                }
            }

            SetThrowingHelpHandlerRecursive(rootCommand);

            int returnCode = rootCommand.Execute(args);

            Assert.Equal(123, returnCode);
        }

        [Theory]
        [InlineData(new[] { "-a", "delta" }, @"Unexpected argument ""delta"".", new string[0])]  // "-a" is not inherited by the subcommand, so the subcommand is not available.
        [InlineData(new[] { "-c", "delta" }, @"Unrecognized option ""-c"".", new string[0])]  // "-c" exists on the subcommand.
        [InlineData(new[] { "delta", "-a" }, @"Unrecognized option ""-a"".", new string[] { "delta" })]  // "-a" is not inherited by the subcommand.
        public static void Execute_OptionInInvalidPosition_ThrowsInvalidCommandLineException(string[] args, string expectedMessage, string[] commandPath)
        {
            var rootCommand = new RootCommand("test");
            {
                var optionA = rootCommand.AddOption('a', null, "");
                var optionB = rootCommand.AddOption('b', null, "", inherited: true);

                var command = rootCommand.AddSubcommand("delta", "");
                {
                    _ = command.AddOption('c', null, "");
                }
            }

            SetThrowingHelpHandlerRecursive(rootCommand);

            var ex = Assert.Throws<InvalidCommandLineException>(() => rootCommand.Execute(args));

            Assert.Equal(expectedMessage, ex.Message);
            Assert.Equal(GetCommand(rootCommand, commandPath), ex.Command);
        }

        [Fact]
        public static void Execute_RequiredShortOptionNotSpecified_ThrowsInvalidCommandLineException()
        {
            var rootCommand = new RootCommand("test");
            {
                var option = rootCommand.AddOption('a', null, "");
                option.Required = true;
            }

            SetThrowingHelpHandlerRecursive(rootCommand);

            var ex = Assert.Throws<InvalidCommandLineException>(() => rootCommand.Execute(Array.Empty<string>()));

            Assert.Equal(@"Missing required option ""-a"".", ex.Message);
            Assert.Equal(rootCommand, ex.Command);
        }

        [Fact]
        public static void Execute_RequiredLongOptionNotSpecified_ThrowsInvalidCommandLineException()
        {
            var rootCommand = new RootCommand("test");
            {
                var option = rootCommand.AddOption('a', "alpha", "");
                option.Required = true;
            }

            SetThrowingHelpHandlerRecursive(rootCommand);

            var ex = Assert.Throws<InvalidCommandLineException>(() => rootCommand.Execute(Array.Empty<string>()));

            Assert.Equal(@"Missing required option ""--alpha"".", ex.Message);
            Assert.Equal(rootCommand, ex.Command);
        }

        [Theory]
        [InlineData(new string[0], 123)]
        [InlineData(new[] { "delta" }, 234)]
        [InlineData(new[] { "echo" }, 345)]
        [InlineData(new[] { "echo", "foxtrot" }, 456)]
        public static void Execute_CommandPath_InvokesCommand(string[] args, int expectedReturnCode)
        {
            var rootCommand = new RootCommand("test");
            rootCommand.SetInvokeHandler(() => 123);
            {
                var commandD = rootCommand.AddSubcommand("delta", "");
                commandD.SetInvokeHandler(() => 234);

                var commandE = rootCommand.AddSubcommand("echo", "");
                commandE.SetInvokeHandler(() => 345);
                {
                    var commandF = commandE.AddSubcommand("foxtrot", "");
                    commandF.SetInvokeHandler(() => 456);
                }
            }

            SetThrowingHelpHandlerRecursive(rootCommand);

            int returnCode = rootCommand.Execute(args);

            Assert.Equal(expectedReturnCode, returnCode);
        }

        [Fact]
        public static void Execute_UnexpectedParameter_ThrowsInvalidCommandLineException()
        {
            var rootCommand = new RootCommand("test");

            SetThrowingHelpHandlerRecursive(rootCommand);

            var ex = Assert.Throws<InvalidCommandLineException>(() => rootCommand.Execute(new[] { "one" }));

            Assert.Equal(@"Unexpected argument ""one"".", ex.Message);
            Assert.Equal(rootCommand, ex.Command);
        }

        [Fact]
        public static void Execute_UnexpectedParameter2_ThrowsInvalidCommandLineException()
        {
            var rootCommand = new RootCommand("test");
            {
                _ = rootCommand.AddParameter("sierra", "");
            }

            SetThrowingHelpHandlerRecursive(rootCommand);

            var ex = Assert.Throws<InvalidCommandLineException>(() => rootCommand.Execute(new[] { "one", "two" }));

            Assert.Equal(@"Unexpected argument ""two"".", ex.Message);
            Assert.Equal(rootCommand, ex.Command);
        }

        [Fact]
        public static void Execute_Parameter_ArgumentIsStoredInParameter()
        {
            var rootCommand = new RootCommand("test");
            {
                var parameter = rootCommand.AddParameter("sierra", "");

                rootCommand.SetInvokeHandler(() =>
                {
                    Assert.Equal("one", parameter.Value);

                    return 123;
                });
            }

            SetThrowingHelpHandlerRecursive(rootCommand);

            int returnCode = rootCommand.Execute(new[] { "one" });

            Assert.Equal(123, returnCode);
        }

        [Fact]
        public static void Execute_Parameters_ArgumentsAreStoredInParameters()
        {
            var rootCommand = new RootCommand("test");
            {
                var parameterS = rootCommand.AddParameter("sierra", "");
                var parameterT = rootCommand.AddParameter("tango", "");

                rootCommand.SetInvokeHandler(() =>
                {
                    Assert.Equal("one", parameterS.Value);
                    Assert.Equal("two", parameterT.Value);

                    return 123;
                });
            }

            SetThrowingHelpHandlerRecursive(rootCommand);

            int returnCode = rootCommand.Execute(new[] { "one", "two" });

            Assert.Equal(123, returnCode);
        }

        [Theory]
        [InlineData(new string[] { "one" }, new string[] { "one" })]
        [InlineData(new string[] { "one", "two" }, new string[] { "one", "two" })]
        public static void Execute_VariadicParameter_ArgumentIsStoredInParameter(string[] args, string[] expectedParameterValues)
        {
            var rootCommand = new RootCommand("test");
            {
                var parameter = rootCommand.AddVariadicParameter("sierra", "");

                rootCommand.SetInvokeHandler(() =>
                {
                    Assert.Equal(expectedParameterValues, parameter.Values);

                    return 123;
                });
            }

            SetThrowingHelpHandlerRecursive(rootCommand);

            int returnCode = rootCommand.Execute(args);

            Assert.Equal(123, returnCode);
        }

        [Theory]
        [InlineData(new string[0], null)]
        [InlineData(new string[] { "one" }, "one")]
        public static void Execute_OptionalParameter_ArgumentIsStoredInParameter(string[] args, string? expectedValueOrNull)
        {
            var rootCommand = new RootCommand("test");
            {
                var parameter = rootCommand.AddParameter("sierra", "");
                parameter.Optional = true;

                rootCommand.SetInvokeHandler(() =>
                {
                    Assert.Equal(expectedValueOrNull, parameter.ValueOrDefault);

                    return 123;
                });
            }

            SetThrowingHelpHandlerRecursive(rootCommand);

            int returnCode = rootCommand.Execute(args);

            Assert.Equal(123, returnCode);
        }

        [Theory]
        [InlineData(new string[0], new string[0])]
        [InlineData(new string[] { "one" }, new string[] { "one" })]
        [InlineData(new string[] { "one", "two" }, new string[] { "one", "two" })]
        public static void Execute_OptionalVariadicParameter_ArgumentIsStoredInParameter(string[] args, string[] expectedParameterValues)
        {
            var rootCommand = new RootCommand("test");
            {
                var parameter = rootCommand.AddVariadicParameter("sierra", "");
                parameter.Optional = true;

                rootCommand.SetInvokeHandler(() =>
                {
                    Assert.Equal(expectedParameterValues, parameter.Values);

                    return 123;
                });
            }

            SetThrowingHelpHandlerRecursive(rootCommand);

            int returnCode = rootCommand.Execute(args);

            Assert.Equal(123, returnCode);
        }

        [Fact]
        public static void Execute_MissingRequiredParameterAfterMissingOptionalParameter_RequiredParameterNotRequiredWithoutOptionalParameter()
        {
            var rootCommand = new RootCommand("test");
            {
                var parameterS = rootCommand.AddParameter("sierra", "");
                parameterS.Optional = true;
                var parameterT = rootCommand.AddParameter("tango", "");

                rootCommand.SetInvokeHandler(() =>
                {
                    Assert.False(parameterS.HasValue);
                    Assert.False(parameterT.HasValue);

                    return 123;
                });
            }

            SetThrowingHelpHandlerRecursive(rootCommand);

            int returnCode = rootCommand.Execute(Array.Empty<string>());

            Assert.Equal(123, returnCode);
        }

        [Fact]
        public static void Execute_MissingRequiredParameterAfterOptionalParameter_ThrowsInvalidCommandLineException()
        {
            var rootCommand = new RootCommand("test");
            {
                var parameterS = rootCommand.AddParameter("sierra", "");
                parameterS.Optional = true;
                var parameterT = rootCommand.AddParameter("tango", "");

                rootCommand.SetInvokeHandler(() =>
                {
                    Assert.True(false);

                    return 123;
                });
            }

            SetThrowingHelpHandlerRecursive(rootCommand);

            var ex = Assert.Throws<InvalidCommandLineException>(() => rootCommand.Execute(new[] { "one" }));

            Assert.Equal(@"Expected additional arguments.", ex.Message);
            Assert.Equal(rootCommand, ex.Command);
        }

        [Fact]
        public static void Execute_InvalidValue_ThrowsInvalidCommandLineException()
        {
            var rootCommand = new RootCommand("test");
            {
                _ = rootCommand.AddParameter<int>("sierra", "", int.TryParse);
            }

            SetThrowingHelpHandlerRecursive(rootCommand);

            var ex = Assert.Throws<InvalidCommandLineException>(() => rootCommand.Execute(new[] { "one" }));

            Assert.Equal($@"Invalid argument ""one"".", ex.Message);
            Assert.Equal(rootCommand, ex.Command);
        }

        [Fact]
        public static void Execute_ExpandWildcardsOnWindowsFalse_WildcardsNotExpanded()
        {
            var rootCommand = new RootCommand("test");
            var parameter = rootCommand.AddVariadicParameter("sierra", "");
            parameter.ExpandWildcardsOnWindows = false;

            string path = typeof(VariadicCommandParameter<>).Assembly.Location;
            string arg = path + "*";

            _ = rootCommand.Execute(new[] { arg });

            Assert.Equal(new[] { arg }, parameter.Values);
        }

        [Fact]
        public static void Execute_ExpandWildcardsOnWindowsTrue_WildcardsExpandedOnWindows()
        {
            var rootCommand = new RootCommand("test");
            var parameter = rootCommand.AddVariadicParameter("sierra", "");
            parameter.ExpandWildcardsOnWindows = true;

            string path = typeof(VariadicCommandParameter<>).Assembly.Location;
            string arg = path + "*";

            _ = rootCommand.Execute(new[] { arg });

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                Assert.Contains(path, parameter.Values);
            else
                Assert.Equal(new[] { arg }, parameter.Values);
        }

        [Fact]
        public static void Execute_InvalidExpandedArgument_ThrowsInvalidCommandLineException()
        {
            var rootCommand = new RootCommand("test");
            {
                var parameter = rootCommand.AddVariadicParameter<int>("sierra", "", int.TryParse);
                parameter.ExpandWildcardsOnWindows = true;
            }

            string path = typeof(VariadicCommandParameter<>).Assembly.Location;
            string arg = path + "*";

            var ex = Assert.Throws<InvalidCommandLineException>(() => rootCommand.Execute(new[] { arg }));

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                Assert.Equal($@"Invalid argument ""{path}"".", ex.Message);
            else
                Assert.Equal($@"Invalid argument ""{arg}"".", ex.Message);
            Assert.Equal(rootCommand, ex.Command);
        }

        [Fact]
        public static void Execute_DoubleDash_TerminatesOptions()
        {
            var rootCommand = new RootCommand("test");
            {
                var parameter = rootCommand.AddParameter("sierra", "");

                rootCommand.SetInvokeHandler(() =>
                {
                    Assert.Equal("-one", parameter.Value);

                    return 123;
                });
            }

            SetThrowingHelpHandlerRecursive(rootCommand);

            int returnCode = rootCommand.Execute(new[] { "--", "-one" });

            Assert.Equal(123, returnCode);
        }

        [Fact]
        public static void Execute_RequiredParametersPartiallyUnspecified_ThrowsInvalidCommandLineException()
        {
            var rootCommand = new RootCommand("test");
            {
                _ = rootCommand.AddParameter("sierra", "");
                _ = rootCommand.AddParameter("tango", "");
            }

            SetThrowingHelpHandlerRecursive(rootCommand);

            var ex = Assert.Throws<InvalidCommandLineException>(() => rootCommand.Execute(new[] { "one" }));

            Assert.Equal(@"Expected additional arguments.", ex.Message);
            Assert.Equal(rootCommand, ex.Command);
        }

        [Fact]
        public static void Execute_RequiredParametersCompletelyUnspecifiedAndOptionSpecified_ThrowsInvalidCommandLineException()
        {
            var rootCommand = new RootCommand("test");
            {
                _ = rootCommand.AddOption('a', null, "");

                _ = rootCommand.AddParameter("sierra", "");
            }

            SetThrowingHelpHandlerRecursive(rootCommand);

            var ex = Assert.Throws<InvalidCommandLineException>(() => rootCommand.Execute(new[] { "-a" }));

            Assert.Equal(@"Expected additional arguments.", ex.Message);
            Assert.Equal(rootCommand, ex.Command);
        }

        [Fact]
        public static void Execute_RequiredParametersCompletelyUnspecifiedAndNoOptionsSpecified_WritesHelp()
        {
            var rootCommand = new RootCommand("test");
            {
                _ = rootCommand.AddParameter("sierra", "yankee");

                rootCommand.SetInvokeHandler(() =>
                {
                    Assert.True(false);

                    return 123;
                });
            }

            var writer = new StringWriter();
            SetHelpHandlerRecursive(rootCommand, writer);

            int returnCode = rootCommand.Execute(Array.Empty<string>());

            Assert.Contains("yankee", writer.ToString());
            Assert.Equal(-1, returnCode);
        }

        [Fact]
        public static void Execute_RequiredParametersCompletelyUnspecifiedAndVerboseOptionSpecified_WritesHelp()
        {
            var rootCommand = new RootCommand("test");
            {
                rootCommand.VerboseOption = rootCommand.AddOption('v', "verbose", "", inherited: true);

                _ = rootCommand.AddParameter("sierra", "yankee");

                rootCommand.SetInvokeHandler(() =>
                {
                    Assert.True(false);

                    return 123;
                });
            }

            var writer = new StringWriter();
            SetHelpHandlerRecursive(rootCommand, writer);

            int returnCode = rootCommand.Execute(new[] { "-v" });

            Assert.Contains("yankee", writer.ToString());
            Assert.Equal(-1, returnCode);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public static void Execute_RequiredParametersCompletelyUnspecifiedWithVerboseOption_WritesHelp(bool verbose)
        {
            var rootCommand = new RootCommand("test");
            {
                rootCommand.VerboseOption = rootCommand.AddOption('v', "verbose", "", inherited: true);

                var parameterS = rootCommand.AddParameter("sierra", "yankee");
                var parameterT = rootCommand.AddParameter("tango", "zulu");
                parameterT.HelpVisibility = HelpVisibility.Verbose;

                rootCommand.SetInvokeHandler(() =>
                {
                    Assert.True(false);

                    return 123;
                });
            }

            var writer = new StringWriter();
            SetHelpHandlerRecursive(rootCommand, writer);

            int returnCode = rootCommand.Execute(verbose ? new[] { "-v" } : Array.Empty<string>());

            Assert.Contains("yankee", writer.ToString());
            Assert.Equal(verbose, writer.ToString().Contains("zulu", StringComparison.Ordinal));
            Assert.Equal(-1, returnCode);
        }

        [Fact]
        public static void Execute_HelpOption_WritesHelp()
        {
            var rootCommand = new RootCommand("test");
            {
                rootCommand.HelpOption = rootCommand.AddOption('h', "help", "", inherited: true);

                _ = rootCommand.AddOption(null, "alpha", "");

                rootCommand.SetInvokeHandler(() =>
                {
                    Assert.True(false);

                    return 123;
                });
            }

            var writer = new StringWriter();
            SetHelpHandlerRecursive(rootCommand, writer);

            int returnCode = rootCommand.Execute(new string[] { "-h" });

            Assert.Contains("alpha", writer.ToString());
            Assert.Equal(0, returnCode);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public static void Execute_HelpOptionWithVerboseOption_WritesHelp(bool verbose)
        {
            var rootCommand = new RootCommand("test");
            {
                rootCommand.HelpOption = rootCommand.AddOption('h', "help", "", inherited: true);

                rootCommand.VerboseOption = rootCommand.AddOption('v', "verbose", "", inherited: true);

                var optionA = rootCommand.AddOption(null, "alpha", "");
                var optionB = rootCommand.AddOption(null, "bravo", "");
                optionB.HelpVisibility = HelpVisibility.Verbose;

                rootCommand.SetInvokeHandler(() =>
                {
                    Assert.True(false);

                    return 123;
                });
            }

            var writer = new StringWriter();
            SetHelpHandlerRecursive(rootCommand, writer);

            int returnCode = rootCommand.Execute(verbose ? new[] { "-h", "-v" } : new[] { "-h" });

            Assert.Contains("alpha", writer.ToString());
            Assert.Equal(verbose, writer.ToString().Contains("bravo", StringComparison.Ordinal));
            Assert.Equal(0, returnCode);
        }

        [Fact]
        public static void Execute_SubcommandHelpOption_WritesHelp()
        {
            var rootCommand = new RootCommand("test");
            {
                var command = rootCommand.AddSubcommand("delta", "");
                {
                    command.HelpOption = command.AddOption('h', "help", "", inherited: true);

                    _ = command.AddOption(null, "alpha", "");

                    command.SetInvokeHandler(() =>
                    {
                        Assert.True(false);

                        return 123;
                    });
                }
            }

            var writer = new StringWriter();
            SetHelpHandlerRecursive(rootCommand, writer);

            int returnCode = rootCommand.Execute(new string[] { "delta", "-h" });

            Assert.Contains("alpha", writer.ToString());
            Assert.Equal(0, returnCode);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public static void Execute_SubcommandHelpOptionWithVerboseOption_WritesHelp(bool verbose)
        {
            var rootCommand = new RootCommand("test");
            {
                var command = rootCommand.AddSubcommand("delta", "");
                {
                    command.HelpOption = command.AddOption('h', "help", "", inherited: true);

                    command.VerboseOption = command.AddOption('v', "verbose", "", inherited: true);

                    var optionA = command.AddOption(null, "alpha", "");
                    var optionB = command.AddOption(null, "bravo", "");
                    optionB.HelpVisibility = HelpVisibility.Verbose;

                    command.SetInvokeHandler(() =>
                    {
                        Assert.True(false);

                        return 123;
                    });
                }
            }

            var writer = new StringWriter();
            SetHelpHandlerRecursive(rootCommand, writer);

            int returnCode = rootCommand.Execute(verbose ? new[] { "delta", "-h", "-v" } : new[] { "delta", "-h" });

            Assert.Contains("alpha", writer.ToString());
            Assert.Equal(verbose, writer.ToString().Contains("bravo", StringComparison.Ordinal));
            Assert.Equal(0, returnCode);
        }

        [Fact]
        public static void Reset_Unused_DoesNothing()
        {
            var rootCommand = new RootCommand("test");

            var optionA = rootCommand.AddOption('a', null, "");

            var optionB = rootCommand.AddOption('b', null, "value", "");

            var optionC = rootCommand.AddVariadicOption('c', null, "");

            var optionD = rootCommand.AddVariadicOption('d', null, "value", "");

            var parameterS = rootCommand.AddParameter("sierra", "");

            var parameterT = rootCommand.AddVariadicParameter("tango", "");

            rootCommand.Reset();

            Assert.Equal(0, optionA.Count);
            Assert.Equal(0, optionB.Count);
            Assert.Throws<InvalidOperationException>(() => optionB.Value);
            Assert.Equal(0, optionC.Count);
            Assert.Equal(0, optionD.Count);
            Assert.Empty(optionD.Values);
            Assert.Equal(0, parameterS.Count);
            Assert.Throws<InvalidOperationException>(() => parameterS.Value);
            Assert.Equal(0, parameterT.Count);
            Assert.Empty(parameterT.Values);
        }

        [Fact]
        public static void Reset_Executed_ResetsParametersAndOptions()
        {
            var rootCommand = new RootCommand("test");

            var optionA = rootCommand.AddOption('a', null, "");

            var optionB = rootCommand.AddOption('b', null, "value", "");

            var optionC = rootCommand.AddVariadicOption('c', null, "value", "");

            var parameterS = rootCommand.AddParameter("sierra", "");

            var parameterT = rootCommand.AddVariadicParameter("tango", "");

            _ = rootCommand.Execute(new[] { "-a", "-b=b", "-c=c", "s", "t" });

            Assert.Equal(1, optionA.Count);
            Assert.Equal(1, optionB.Count);
            Assert.NotNull(optionB.Value);
            Assert.Equal(1, optionC.Count);
            Assert.Single(optionC.Values);
            Assert.Equal(1, parameterS.Count);
            Assert.NotNull(parameterS.Value);
            Assert.Equal(1, parameterT.Count);
            Assert.Single(parameterT.Values);

            rootCommand.Reset();

            Assert.Equal(0, optionA.Count);
            Assert.Equal(0, optionB.Count);
            Assert.Throws<InvalidOperationException>(() => optionB.Value);
            Assert.Equal(0, optionC.Count);
            Assert.Empty(optionC.Values);
            Assert.Equal(0, parameterS.Count);
            Assert.Throws<InvalidOperationException>(() => parameterS.Value);
            Assert.Equal(0, parameterT.Count);
            Assert.Empty(parameterT.Values);
        }

        [Fact]
        public static void Reset_ExecutedSubcommand_ResetsSubcommand()
        {
            var rootCommand = new RootCommand("test");

            var optionA = rootCommand.AddOption('a', null, "", inherited: true);

            var commandE = rootCommand.AddSubcommand("echo", "");

            var optionB = commandE.AddOption('b', null, "");

            _ = rootCommand.Execute(new[] { "echo", "-a", "-b" });

            Assert.Equal(1, optionA.Count);
            Assert.Equal(1, optionB.Count);

            rootCommand.Reset();

            Assert.Equal(0, optionA.Count);
            Assert.Equal(0, optionB.Count);
        }

        private static void SetThrowingHelpHandlerRecursive(Command command)
        {
            command.SetHelpHandler((command, verbose) => throw new Exception());

            foreach (var subcommand in command.Subcommands)
                SetThrowingHelpHandlerRecursive(subcommand);
        }

        private static void SetHelpHandlerRecursive(Command command, StringWriter writer)
        {
            command.SetHelpHandler((command, verbose) => CommandHelp.WriteHelp(command, writer, verbose));

            foreach (var subcommand in command.Subcommands)
                SetHelpHandlerRecursive(subcommand, writer);
        }

        private static Command GetCommand(Command command, string[] path)
        {
            foreach (string commandName in path)
                command = GetSubcommand(command, commandName);
            return command;

            static Command GetSubcommand(Command command, string subcommandName)
            {
                foreach (var subcommand in command.Subcommands)
                    if (subcommand.Name.Equals(subcommandName, StringComparison.InvariantCulture))
                        return subcommand;

                throw new InvalidOperationException("No such command.");
            }
        }
    }
}
