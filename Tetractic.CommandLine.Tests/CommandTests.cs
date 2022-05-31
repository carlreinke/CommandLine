// Copyright 2022 Carl Reinke
//
// This file is part of a library that is licensed under the terms of GNU Lesser
// General Public License version 3 as published by the Free Software
// Foundation.
//
// This license does not grant rights under trademark law for use of any trade
// names, trademarks, or service marks.

using System;
using System.Threading.Tasks;
using Xunit;

namespace Tetractic.CommandLine.Tests
{
    public static class CommandTests
    {
        [Fact]
        public static void Parent_RootCommand_ReturnsNull()
        {
            var rootCommand = new RootCommand("test");

            Assert.Null(rootCommand.Parent);
        }

        [Fact]
        public static void Parent_Subcommand_ReturnsParentCommand()
        {
            var rootCommand = new RootCommand("test");
            var subcommand1 = rootCommand.AddSubcommand("delta", "");
            var subcommand2 = subcommand1.AddSubcommand("echo", "");

            Assert.Same(rootCommand, subcommand1.Parent);
            Assert.Same(subcommand1, subcommand2.Parent);
        }

        [Fact]
        public static void Name_RootCommand_ReturnsName()
        {
            var rootCommand = new RootCommand("test");

            Assert.Equal("test", rootCommand.Name);
        }

        [Fact]
        public static void Name_Subcommand_ReturnsName()
        {
            var rootCommand = new RootCommand("test");
            var subcommand = rootCommand.AddSubcommand("delta", "");

            Assert.Equal("delta", subcommand.Name);
        }

        [Fact]
        public static void Description_RootCommand_ReturnsDescription()
        {
            var rootCommand = new RootCommand("test");

            Assert.Equal("", rootCommand.Description);
        }

        [Fact]
        public static void Description_Subcommand_ReturnsDescription()
        {
            var rootCommand = new RootCommand("test");
            var subcommand = rootCommand.AddSubcommand("delta", "yankee");

            Assert.Equal("yankee", subcommand.Description);
        }

        [Fact]
        public static void HelpVisibility_InitializedRootCommand_ReturnsAlways()
        {
            var rootCommand = new RootCommand("test");

            Assert.Equal(HelpVisibility.Always, rootCommand.HelpVisibility);
        }

        [Fact]
        public static void HelpVisibility_InitializedSubcommand_ReturnsAlways()
        {
            var rootCommand = new RootCommand("test");
            var subcommand = rootCommand.AddSubcommand("delta", "");

            Assert.Equal(HelpVisibility.Always, subcommand.HelpVisibility);
        }

        [Fact]
        public static void HelpOption_InitializedRootCommand_ReturnsNull()
        {
            var rootCommand = new RootCommand("test");

            Assert.Null(rootCommand.HelpOption);
        }

        [Fact]
        public static void HelpOption_InitializedSubcommand_ReturnsNull()
        {
            var rootCommand = new RootCommand("test");
            var subcommand = rootCommand.AddSubcommand("delta", "");

            Assert.Null(subcommand.HelpOption);
        }

        [Fact]
        public static void HelpOption_OptionIsNull_UnsetsHelpOption()
        {
            var rootCommand = new RootCommand("test");
            rootCommand.HelpOption = rootCommand.AddOption('h', "help", "Shows help.");

            rootCommand.HelpOption = null;

            Assert.Null(rootCommand.HelpOption);
        }

        [Fact]
        public static void HelpOption_OptionIsFromDifferentCommand_ThrowsInvalidOperationException()
        {
            var rootCommand = new RootCommand("test");
            rootCommand.HelpOption = rootCommand.AddOption('h', "help", "Shows help.");
            var command = rootCommand.AddSubcommand("delta", "");

            var ex = Assert.Throws<InvalidOperationException>(() => command.HelpOption = rootCommand.HelpOption);

            Assert.Equal("The command option does not exist on the command.", ex.Message);
        }

        [Fact]
        public static void VerboseOption_InitializedRootCommand_ReturnsNull()
        {
            var rootCommand = new RootCommand("test");

            Assert.Null(rootCommand.VerboseOption);
        }

        [Fact]
        public static void VerboseOption_InitializedSubcommand_ReturnsNull()
        {
            var rootCommand = new RootCommand("test");
            var subcommand = rootCommand.AddSubcommand("delta", "");

            Assert.Null(subcommand.VerboseOption);
        }

        [Fact]
        public static void VerboseOption_OptionIsNull_UnsetsVerboseOption()
        {
            var rootCommand = new RootCommand("test");
            rootCommand.VerboseOption = rootCommand.AddOption('v', "verbose", "Enables verbose output.");

            rootCommand.VerboseOption = null;

            Assert.Null(rootCommand.VerboseOption);
        }

        [Fact]
        public static void VerboseOption_OptionIsFromDifferentCommand_ThrowsInvalidOperationException()
        {
            var rootCommand = new RootCommand("test");
            rootCommand.VerboseOption = rootCommand.AddOption('v', "verbose", "Enables verbose output.");
            var command = rootCommand.AddSubcommand("delta", "");

            var ex = Assert.Throws<InvalidOperationException>(() => command.VerboseOption = rootCommand.VerboseOption);

            Assert.Equal("The command option does not exist on the command.", ex.Message);
        }

        [Fact]
        public static void Subcommands_Initialized_ReturnsEmpty()
        {
            var rootCommand = new RootCommand("test");

            Assert.Empty(rootCommand.Subcommands);
        }

        [Fact]
        public static void Subcommands_Always_ReturnsSubcommands()
        {
            var rootCommand = new RootCommand("test");
            var subcommand = rootCommand.AddSubcommand("delta", "");

            Assert.Equal(new[] { subcommand }, rootCommand.Subcommands);
        }

        [Fact]
        public static void Parameters_Initialized_ReturnsEmpty()
        {
            var rootCommand = new RootCommand("test");

            Assert.Empty(rootCommand.Parameters);
        }

        [Fact]
        public static void Parameters_Always_ReturnsParameters()
        {
            var rootCommand = new RootCommand("test");
            var parameter = rootCommand.AddParameter("sierra", "");

            Assert.Equal(new[] { parameter }, rootCommand.Parameters);
        }

        [Fact]
        public static void Options_Initialized_ReturnsEmpty()
        {
            var rootCommand = new RootCommand("test");

            Assert.Empty(rootCommand.Parameters);
        }

        [Fact]
        public static void Options_Always_ReturnsOptions()
        {
            var rootCommand = new RootCommand("test");
            var option = rootCommand.AddOption(null, "alpha", "");

            Assert.Equal(new[] { option }, rootCommand.Options);
        }

        [Fact]
        public static void Options_Always_DoesNotReturnInheritedOptions()
        {
            var rootCommand = new RootCommand("test");
            _ = rootCommand.AddOption(null, "alpha", "", inherited: true);
            var subcommand = rootCommand.AddSubcommand("delta", "");

            Assert.Empty(subcommand.Options);
        }

        [Fact]
        public static void AddSubcommand_NameIsNull_ThrowsArgumentNullException()
        {
            var rootCommand = new RootCommand("test");

            var ex = Assert.Throws<ArgumentNullException>(() => rootCommand.AddSubcommand(null!, ""));

            Assert.Equal("name", ex.ParamName);
        }

        [Theory]
        [InlineData("")]
        [InlineData("delta echo")]
        [InlineData("-delta")]
        public static void AddSubcommand_NameIsInvalid_ThrowsArgumentException(string name)
        {
            var rootCommand = new RootCommand("test");

            var ex = Assert.Throws<ArgumentException>(() => rootCommand.AddSubcommand(name, ""));

            Assert.StartsWith("Invalid name.", ex.Message);
            Assert.Equal("name", ex.ParamName);
        }

        [Fact]
        public static void AddSubcommand_DescriptionIsNull_ThrowsArgumentNullException()
        {
            var rootCommand = new RootCommand("test");

            var ex = Assert.Throws<ArgumentNullException>(() => rootCommand.AddSubcommand("delta", null!));

            Assert.Equal("description", ex.ParamName);
        }

        [Fact]
        public static void AddSubcommand_NameIsDuplicate_ThrowsInvalidOperationException()
        {
            var rootCommand = new RootCommand("test");
            _ = rootCommand.AddSubcommand("delta", "");

            var ex = Assert.Throws<InvalidOperationException>(() => rootCommand.AddSubcommand("delta", ""));
        }

        [Fact]
        public static void AddParameter_NameIsNull_ThrowsArgumentNullException()
        {
            var rootCommand = new RootCommand("test");

            var ex = Assert.Throws<ArgumentNullException>(() => rootCommand.AddParameter(null!, ""));

            Assert.Equal("name", ex.ParamName);
        }

        [Theory]
        [InlineData("")]
        [InlineData("sierra tango")]
        public static void AddParameter_NameIsEmpty_ThrowsArgumentException(string name)
        {
            var rootCommand = new RootCommand("test");

            var ex = Assert.Throws<ArgumentException>(() => rootCommand.AddParameter(name, ""));

            Assert.StartsWith("Invalid name.", ex.Message);
            Assert.Equal("name", ex.ParamName);
        }

        [Fact]
        public static void AddParameter_DescriptionIsNull_ThrowsArgumentNullException()
        {
            var rootCommand = new RootCommand("test");

            var ex = Assert.Throws<ArgumentNullException>(() => rootCommand.AddParameter("sierra", null!));

            Assert.Equal("description", ex.ParamName);
        }

        [Fact]
        public static void AddParameter_ParseIsNull_ThrowsArgumentNullException()
        {
            var rootCommand = new RootCommand("test");

            var ex = Assert.Throws<ArgumentNullException>(() => rootCommand.AddParameter<int>("sierra", "", null!));

            Assert.Equal("parse", ex.ParamName);
        }

        [Fact]
        public static void AddParameter_NameIsDuplicate_ThrowsInvalidOperationException()
        {
            var rootCommand = new RootCommand("test");
            _ = rootCommand.AddParameter("sierra", "");

            var ex = Assert.Throws<InvalidOperationException>(() => rootCommand.AddParameter("sierra", ""));

            Assert.Equal("The command already has a parameter with the same name.", ex.Message);
        }

        [Fact]
        public static void AddParameter_HasVariadicParameter_ThrowsInvalidOperationException()
        {
            var rootCommand = new RootCommand("test");
            _ = rootCommand.AddVariadicParameter("sierra", "");

            var ex = Assert.Throws<InvalidOperationException>(() => rootCommand.AddParameter("tango", ""));

            Assert.Equal("The command has a variadic parameter, which must be the last parameter.", ex.Message);
        }

        [Fact]
        public static void AddVariadicParameter_NameIsNull_ThrowsArgumentNullException()
        {
            var rootCommand = new RootCommand("test");

            var ex = Assert.Throws<ArgumentNullException>(() => rootCommand.AddVariadicParameter(null!, ""));

            Assert.Equal("name", ex.ParamName);
        }

        [Theory]
        [InlineData("")]
        [InlineData("sierra tango")]
        public static void AddVariadicParameter_NameIsEmpty_ThrowsArgumentException(string name)
        {
            var rootCommand = new RootCommand("test");

            var ex = Assert.Throws<ArgumentException>(() => rootCommand.AddVariadicParameter(name, ""));

            Assert.StartsWith("Invalid name.", ex.Message);
            Assert.Equal("name", ex.ParamName);
        }

        [Fact]
        public static void AddVariadicParameter_DescriptionIsNull_ThrowsArgumentNullException()
        {
            var rootCommand = new RootCommand("test");

            var ex = Assert.Throws<ArgumentNullException>(() => rootCommand.AddVariadicParameter("sierra", null!));

            Assert.Equal("description", ex.ParamName);
        }

        [Fact]
        public static void AddVariadicParameter_ParseIsNull_ThrowsArgumentNullException()
        {
            var rootCommand = new RootCommand("test");

            var ex = Assert.Throws<ArgumentNullException>(() => rootCommand.AddVariadicParameter<int>("sierra", "", null!));

            Assert.Equal("parse", ex.ParamName);
        }

        [Fact]
        public static void AddVariadicParameter_NameIsDuplicate_ThrowsInvalidOperationException()
        {
            var rootCommand = new RootCommand("test");
            _ = rootCommand.AddParameter("sierra", "");

            var ex = Assert.Throws<InvalidOperationException>(() => rootCommand.AddVariadicParameter("sierra", ""));

            Assert.Equal("The command already has a parameter with the same name.", ex.Message);
        }

        [Fact]
        public static void AddVariadicParameter_HasVariadicParameter_ThrowsInvalidOperationException()
        {
            var rootCommand = new RootCommand("test");
            _ = rootCommand.AddVariadicParameter("sierra", "");

            var ex = Assert.Throws<InvalidOperationException>(() => rootCommand.AddVariadicParameter("tango", ""));

            Assert.Equal("The command has a variadic parameter, which must be the last parameter.", ex.Message);
        }

        [Theory]
        [InlineData('u')]
        [InlineData('p')]
        public static void AddOption_ShortNameAndLongNameAreNull_ThrowsArgumentException(char overload)
        {
            var rootCommand = new RootCommand("test");

            var ex = overload switch
            {
                'u' => Assert.Throws<ArgumentException>(() => rootCommand.AddOption(null, null, "")),
                'p' => Assert.Throws<ArgumentException>(() => rootCommand.AddOption(null, null, "value", "")),
                _ => throw new NotImplementedException(),
            };

            Assert.Equal("No names were specified.", ex.Message);
            Assert.Null(ex.ParamName);
        }

        [Theory]
        [InlineData('u', ' ')]
        [InlineData('u', '-')]
        [InlineData('u', '=')]
        [InlineData('u', '\uD800')]
        [InlineData('u', '\uDC00')]
        [InlineData('p', ' ')]
        [InlineData('p', '-')]
        [InlineData('p', '=')]
        [InlineData('p', '\uD800')]
        [InlineData('p', '\uDC00')]
        public static void AddOption_ShortNameIsInvalid_ThrowsArgumentException(char overload, char shortName)
        {
            var rootCommand = new RootCommand("test");

            var ex = overload switch
            {
                'u' => Assert.Throws<ArgumentException>(() => rootCommand.AddOption(shortName, null, "")),
                'p' => Assert.Throws<ArgumentException>(() => rootCommand.AddOption(shortName, null, "value", "")),
                _ => throw new NotImplementedException(),
            };

            Assert.StartsWith("Invalid name.", ex.Message);
            Assert.Equal("shortName", ex.ParamName);
        }

        [Theory]
        [InlineData('u', "")]
        [InlineData('u', "alpha bravo")]
        [InlineData('u', "alpha=value")]
        [InlineData('p', "")]
        [InlineData('p', "alpha bravo")]
        [InlineData('p', "alpha=value")]
        public static void AddOption_LongNameIsInvalid_ThrowsArgumentException(char overload, string longName)
        {
            var rootCommand = new RootCommand("test");

            var ex = overload switch
            {
                'u' => Assert.Throws<ArgumentException>(() => rootCommand.AddOption(null, longName, "")),
                'p' => Assert.Throws<ArgumentException>(() => rootCommand.AddOption(null, longName, "value", "")),
                _ => throw new NotImplementedException(),
            };

            Assert.StartsWith("Invalid name.", ex.Message);
            Assert.Equal("longName", ex.ParamName);
        }

        [Fact]
        public static void AddOption_ParameterNameIsNull_ThrowsArgumentNullException()
        {
            var rootCommand = new RootCommand("test");

            var ex = Assert.Throws<ArgumentNullException>(() => rootCommand.AddOption('a', "alpha", null!, ""));

            Assert.Equal("parameterName", ex.ParamName);
        }

        [Theory]
        [InlineData("")]
        [InlineData("a value")]
        public static void AddOption_ParameterNameIsInvalid_ThrowsArgumentException(string parameterName)
        {
            var rootCommand = new RootCommand("test");

            var ex = Assert.Throws<ArgumentException>(() => rootCommand.AddOption('a', "alpha", parameterName, ""));

            Assert.StartsWith("Invalid name.", ex.Message);
            Assert.Equal("parameterName", ex.ParamName);
        }

        [Theory]
        [InlineData('u')]
        [InlineData('p')]
        public static void AddOption_DescriptionIsNull_ThrowsArgumentNullException(char overload)
        {
            var rootCommand = new RootCommand("test");

            var ex = overload switch
            {
                'u' => Assert.Throws<ArgumentNullException>(() => rootCommand.AddOption('a', "alpha", null!)),
                'p' => Assert.Throws<ArgumentNullException>(() => rootCommand.AddOption('a', "alpha", "value", null!)),
                _ => throw new NotImplementedException(),
            };

            Assert.Equal("description", ex.ParamName);
        }

        [Fact]
        public static void AddOption_ParseIsNull_ThrowsArgumentNullException()
        {
            var rootCommand = new RootCommand("test");

            var ex = Assert.Throws<ArgumentNullException>(() => rootCommand.AddOption<int>('a', "alpha", "value", "", null!));

            Assert.Equal("parse", ex.ParamName);
        }

        [Theory]
        [InlineData('u')]
        [InlineData('p')]
        public static void AddOption_ShortNameIsDuplicate_ThrowsInvalidOperationException(char overload)
        {
            var rootCommand = new RootCommand("test");
            _ = rootCommand.AddOption('a', null, "");

            var ex = overload switch
            {
                'u' => Assert.Throws<InvalidOperationException>(() => rootCommand.AddOption('a', "alpha", "")),
                'p' => Assert.Throws<InvalidOperationException>(() => rootCommand.AddOption('a', "alpha", "value", "")),
                _ => throw new NotImplementedException(),
            };

            Assert.Equal("The command already has an option with the same short name.", ex.Message);
        }

        [Theory]
        [InlineData('u')]
        [InlineData('p')]
        public static void AddOption_LongNameIsDuplicate_ThrowsInvalidOperationException(char overload)
        {
            var rootCommand = new RootCommand("test");
            _ = rootCommand.AddOption(null, "alpha", "");

            var ex = overload switch
            {
                'u' => Assert.Throws<InvalidOperationException>(() => rootCommand.AddOption('a', "alpha", "")),
                'p' => Assert.Throws<InvalidOperationException>(() => rootCommand.AddOption('a', "alpha", "value", "")),
                _ => throw new NotImplementedException(),
            };

            Assert.Equal("The command already has an option with the same long name.", ex.Message);
        }

        [Fact]
        public static void AddVariadicOption_ShortNameAndLongNameAreNull_ThrowsArgumentNullException()
        {
            var rootCommand = new RootCommand("test");

            var ex = Assert.Throws<ArgumentException>(() => rootCommand.AddVariadicOption(null, null, "value", ""));

            Assert.Equal("No names were specified.", ex.Message);
            Assert.Null(ex.ParamName);
        }

        [Theory]
        [InlineData(' ')]
        [InlineData('-')]
        [InlineData('=')]
        [InlineData('\uD800')]
        [InlineData('\uDC00')]
        public static void AddVariadicOption_ShortNameIsInvalid_ThrowsArgumentException(char shortName)
        {
            var rootCommand = new RootCommand("test");

            var ex = Assert.Throws<ArgumentException>(() => rootCommand.AddVariadicOption(shortName, null, "value", ""));

            Assert.StartsWith("Invalid name.", ex.Message);
            Assert.Equal("shortName", ex.ParamName);
        }

        [Theory]
        [InlineData("")]
        [InlineData("alpha bravo")]
        [InlineData("alpha=value")]
        public static void AddVariadicOption_LongNameIsInvalid_ThrowsArgumentException(string longName)
        {
            var rootCommand = new RootCommand("test");

            var ex = Assert.Throws<ArgumentException>(() => rootCommand.AddVariadicOption(null, longName, "value", ""));

            Assert.StartsWith("Invalid name.", ex.Message);
            Assert.Equal("longName", ex.ParamName);
        }

        [Fact]
        public static void AddVariadicOption_ParameterNameIsNull_ThrowsArgumentNullException()
        {
            var rootCommand = new RootCommand("test");

            var ex = Assert.Throws<ArgumentNullException>(() => rootCommand.AddVariadicOption('a', "alpha", null!, ""));

            Assert.Equal("parameterName", ex.ParamName);
        }

        [Theory]
        [InlineData("")]
        [InlineData("a value")]
        public static void AddVariadicOption_ParameterNameIsInvalid_ThrowsArgumentException(string parameterName)
        {
            var rootCommand = new RootCommand("test");

            var ex = Assert.Throws<ArgumentException>(() => rootCommand.AddVariadicOption('a', "alpha", parameterName, ""));

            Assert.StartsWith("Invalid name.", ex.Message);
            Assert.Equal("parameterName", ex.ParamName);
        }

        [Fact]
        public static void AddVariadicOption_DescriptionIsNull_ThrowsArgumentNullException()
        {
            var rootCommand = new RootCommand("test");

            var ex = Assert.Throws<ArgumentNullException>(() => rootCommand.AddVariadicOption('a', "alpha", "value", null!));

            Assert.Equal("description", ex.ParamName);
        }

        [Fact]
        public static void AddVariadicOption_ParseIsNull_ThrowsArgumentNullException()
        {
            var rootCommand = new RootCommand("test");

            var ex = Assert.Throws<ArgumentNullException>(() => rootCommand.AddVariadicOption<int>('a', "alpha", "value", "", null!));

            Assert.Equal("parse", ex.ParamName);
        }

        [Fact]
        public static void AddVariadicOption_ShortNameIsDuplicate_ThrowsInvalidOperationException()
        {
            var rootCommand = new RootCommand("test");
            _ = rootCommand.AddVariadicOption('a', "alpha", "value", "");

            var ex = Assert.Throws<InvalidOperationException>(() => rootCommand.AddVariadicOption('a', null, "value", ""));

            Assert.Equal("The command already has an option with the same short name.", ex.Message);
        }

        [Fact]
        public static void AddVariadicOption_LongNameIsDuplicate_ThrowsInvalidOperationException()
        {
            var rootCommand = new RootCommand("test");
            _ = rootCommand.AddVariadicOption('a', "alpha", "value", "");

            var ex = Assert.Throws<InvalidOperationException>(() => rootCommand.AddVariadicOption(null, "alpha", "value", ""));

            Assert.Equal("The command already has an option with the same long name.", ex.Message);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public static void AddVariadicOption_ShortNameIsDuplicateOfInherited_ThrowsInvalidOperationException(bool deeper)
        {
            var rootCommand = new RootCommand("test");
            _ = rootCommand.AddOption('a', "alpha", "", inherited: true);
            var subcommand = rootCommand.AddSubcommand("delta", "");
            if (deeper)
                subcommand = subcommand.AddSubcommand("echo", "");

            var ex = Assert.Throws<InvalidOperationException>(() => subcommand.AddVariadicOption('a', null, "value", ""));

            Assert.Equal("The command already inherits an option with the same short name.", ex.Message);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public static void AddVariadicOption_ShortNameIsDuplicateOfUninherited_NoThrow(bool deeper)
        {
            var rootCommand = new RootCommand("test");
            _ = rootCommand.AddOption('a', "alpha", "", inherited: false);
            var subcommand = rootCommand.AddSubcommand("delta", "");
            if (deeper)
                subcommand = subcommand.AddSubcommand("echo", "");

            _ = subcommand.AddVariadicOption('a', null, "value", "");
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public static void AddVariadicOption_ShortNameIsNotDuplicateOfInherited_NoThrow(bool deeper)
        {
            var rootCommand = new RootCommand("test");
            _ = rootCommand.AddOption(null, "bravo", "", inherited: true);
            _ = rootCommand.AddOption('c', null, "", inherited: true);
            var subcommand = rootCommand.AddSubcommand("delta", "");
            if (deeper)
                subcommand = subcommand.AddSubcommand("echo", "");

            _ = subcommand.AddVariadicOption('a', null, "value", "");
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public static void AddVariadicOption_LongNameIsDuplicateOfInherited_ThrowsInvalidOperationException(bool deeper)
        {
            var rootCommand = new RootCommand("test");
            _ = rootCommand.AddOption('a', "alpha", "", inherited: true);
            var subcommand = rootCommand.AddSubcommand("delta", "");
            if (deeper)
                subcommand = subcommand.AddSubcommand("echo", "");

            var ex = Assert.Throws<InvalidOperationException>(() => subcommand.AddVariadicOption(null, "alpha", "value", ""));

            Assert.Equal("The command already inherits an option with the same long name.", ex.Message);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public static void AddVariadicOption_LongNameIsDuplicateOfUninherited_NoThrow(bool deeper)
        {
            var rootCommand = new RootCommand("test");
            _ = rootCommand.AddOption('a', "alpha", "", inherited: false);
            var subcommand = rootCommand.AddSubcommand("delta", "");
            if (deeper)
                subcommand = subcommand.AddSubcommand("echo", "");

            _ = subcommand.AddVariadicOption(null, "alpha", "value", "");
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public static void AddVariadicOption_LongNameIsNotDuplicateOfInherited_NoThrow(bool deeper)
        {
            var rootCommand = new RootCommand("test");
            _ = rootCommand.AddOption(null, "bravo", "", inherited: true);
            _ = rootCommand.AddOption('c', null, "", inherited: true);
            var subcommand = rootCommand.AddSubcommand("delta", "");
            if (deeper)
                subcommand = subcommand.AddSubcommand("echo", "");

            _ = subcommand.AddVariadicOption(null, "alpha", "value", "");
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public static void AddVariadicOption_ShortNameIsDuplicateOfInheritor_ThrowsInvalidOperationException(bool deeper)
        {
            var rootCommand = new RootCommand("test");
            var subcommand = rootCommand.AddSubcommand("delta", "");
            if (deeper)
                subcommand = subcommand.AddSubcommand("echo", "");
            _ = subcommand.AddOption('a', "alpha", "");

            var ex = Assert.Throws<InvalidOperationException>(() => rootCommand.AddVariadicOption('a', null, "value", "", inherited: true));

            Assert.Equal("A subcommand already has an option with the same short name.", ex.Message);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public static void AddVariadicOption_ShortNameIsDuplicateOfInheritorButUninherited_NoThrow(bool deeper)
        {
            var rootCommand = new RootCommand("test");
            var subcommand = rootCommand.AddSubcommand("delta", "");
            if (deeper)
                subcommand = subcommand.AddSubcommand("echo", "");
            _ = subcommand.AddOption('a', "alpha", "");

            _ = rootCommand.AddVariadicOption('a', null, "value", "", inherited: false);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public static void AddVariadicOption_ShortNameIsInheritedButNotDuplicateOfInheritor_NoThrow(bool deeper)
        {
            var rootCommand = new RootCommand("test");
            var subcommand = rootCommand.AddSubcommand("delta", "");
            if (deeper)
                subcommand = subcommand.AddSubcommand("echo", "");
            _ = subcommand.AddOption(null, "bravo", "");
            _ = subcommand.AddOption('c', null, "");

            _ = rootCommand.AddVariadicOption('a', null, "value", "", inherited: true);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public static void AddVariadicOption_LongNameIsDuplicateOfInheritor_ThrowsInvalidOperationException(bool deeper)
        {
            var rootCommand = new RootCommand("test");
            var subcommand = rootCommand.AddSubcommand("delta", "");
            if (deeper)
                subcommand = subcommand.AddSubcommand("echo", "");
            _ = subcommand.AddOption('a', "alpha", "");

            var ex = Assert.Throws<InvalidOperationException>(() => rootCommand.AddVariadicOption(null, "alpha", "value", "", inherited: true));

            Assert.Equal("A subcommand already has an option with the same long name.", ex.Message);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public static void AddVariadicOption_LongNameIsDuplicateOfInheritorButUninherited_NoThrow(bool deeper)
        {
            var rootCommand = new RootCommand("test");
            var subcommand = rootCommand.AddSubcommand("delta", "");
            if (deeper)
                subcommand = subcommand.AddSubcommand("echo", "");
            _ = subcommand.AddOption('a', "alpha", "", inherited: false);

            _ = rootCommand.AddVariadicOption(null, "alpha", "value", "");
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public static void AddVariadicOption_LongNameIsInheritedButNotDuplicateOfInheritor_NoThrow(bool deeper)
        {
            var rootCommand = new RootCommand("test");
            var subcommand = rootCommand.AddSubcommand("delta", "");
            if (deeper)
                subcommand = subcommand.AddSubcommand("echo", "");
            _ = subcommand.AddOption(null, "bravo", "");
            _ = subcommand.AddOption('c', null, "");

            _ = rootCommand.AddVariadicOption(null, "alpha", "value", "", inherited: true);
        }

        [Fact]
        public static void SetInvokeHandler_InvokeIsNull_ThrowsArgumentNullException()
        {
            var rootCommand = new RootCommand("test");

            var ex = Assert.Throws<ArgumentNullException>(() => rootCommand.SetInvokeHandler((Func<int>)null!));

            Assert.Equal("invoke", ex.ParamName);
        }

        [Fact]
        public static void SetInvokeHandler_InvokeIsNull2_ThrowsArgumentNullException()
        {
            var rootCommand = new RootCommand("test");

            var ex = Assert.Throws<ArgumentNullException>(() => rootCommand.SetInvokeHandler((Func<Task<int>>)null!));

            Assert.Equal("invoke", ex.ParamName);
        }

        [Fact]
        public static void Invoke_DefaultInvokeHandler_ReturnsNegative1()
        {
            var rootCommand = new RootCommand("test");

            int returnCode = rootCommand.Invoke();

            Assert.Equal(-1, returnCode);
        }

        [Fact]
        public static void Invoke_InvokeHandlerWasSet_HandlerIsInvoked()
        {
            var rootCommand = new RootCommand("test");
            rootCommand.SetInvokeHandler(() =>
            {
                return 123;
            });

            int returnCode = rootCommand.Invoke();

            Assert.Equal(123, returnCode);
        }

        [Fact]
        public static void Invoke_InvokeHandlerWasSet2_HandlerIsInvoked()
        {
            var rootCommand = new RootCommand("test");
            rootCommand.SetInvokeHandler(() =>
            {
                return Task.FromResult(123);
            });

            int returnCode = rootCommand.Invoke();

            Assert.Equal(123, returnCode);
        }

        [Fact]
        public static void SetHelpHandler_WriteHelpIsNull_ThrowsArgumentNullException()
        {
            var rootCommand = new RootCommand("test");

            var ex = Assert.Throws<ArgumentNullException>(() => rootCommand.SetHelpHandler(null!));

            Assert.Equal("writeHelp", ex.ParamName);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public static void WriteHelp_HelpHandlerWasSet_HandlerIsInvoked(bool verbose)
        {
            bool expectedVerbose = verbose;

            bool invokedHandler = false;

            var rootCommand = new RootCommand("test");
            rootCommand.SetHelpHandler((command, verbose) =>
            {
                Assert.Equal(expectedVerbose, verbose);

                invokedHandler = true;
            });

            rootCommand.WriteHelp(verbose);

            Assert.True(invokedHandler);
        }
    }
}
