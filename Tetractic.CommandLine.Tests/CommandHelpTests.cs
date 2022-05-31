// Copyright 2022 Carl Reinke
//
// This file is part of a library that is licensed under the terms of GNU Lesser
// General Public License version 3 as published by the Free Software
// Foundation.
//
// This license does not grant rights under trademark law for use of any trade
// names, trademarks, or service marks.

using System;
using System.IO;
using Xunit;

namespace Tetractic.CommandLine.Tests
{
    [Trait("Category", "CommandHelp")]
    public static class CommandHelpTests
    {
        [Fact]
        public static void WriteHelp_CommandIsNull_ThrowsArgumentNullException()
        {
            var ex = Assert.Throws<ArgumentNullException>(() =>
            {
                using (var writer = new StringWriter())
                    CommandHelp.WriteHelp(null!, writer, false);
            });

            Assert.Equal("command", ex.ParamName);
        }

        [Fact]
        public static void WriteHelp_WriterIsNull_ThrowsArgumentNullException()
        {
            var rootCommand = new RootCommand("test");

            var ex = Assert.Throws<ArgumentNullException>(() => CommandHelp.WriteHelp(rootCommand, null!, false));

            Assert.Equal("writer", ex.ParamName);
        }

        [Fact]
        public static void WriteHelp_MaxWidthLessThan80_ThrowsArgumentNullException()
        {
            var rootCommand = new RootCommand("test");

            var ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                using (var writer = new StringWriter())
                    CommandHelp.WriteHelp(rootCommand, writer, false, 79);
            });

            Assert.Equal("maxWidth", ex.ParamName);
        }

        [Fact]
        public static void WriteHelp_Initialized_WritesExpectedHelpText()
        {
            var rootCommand = new RootCommand("test");

            string expectedText =
@"Usage: test
";

            string expectedVerboseText = expectedText;

            AssertHelpText(rootCommand, expectedText, expectedVerboseText);
        }

        [Fact]
        public static void WriteHelp_HasAlwaysVisibleSubcommand_WritesExpectedHelpText()
        {
            var rootCommand = new RootCommand("test");
            _ = rootCommand.AddSubcommand("delta", "Performs the delta operation.");

            string expectedText =
@"Usage: test <command>

Commands:
  delta  Performs the delta operation.
";

            string expectedVerboseText = expectedText;

            AssertHelpText(rootCommand, expectedText, expectedVerboseText);
        }

        [Fact]
        public static void WriteHelp_HasVerboseVisibleSubcommand_WritesExpectedHelpText()
        {
            var rootCommand = new RootCommand("test");
            var subcommand = rootCommand.AddSubcommand("delta", "Performs the delta operation.");
            subcommand.HelpVisibility = HelpVisibility.Verbose;

            string expectedText =
@"Usage: test
";

            string expectedVerboseText =
@"Usage: test <command>

Commands:
  delta  Performs the delta operation.
";

            AssertHelpText(rootCommand, expectedText, expectedVerboseText);
        }

        [Fact]
        public static void WriteHelp_HasVerboseVisibleSubcommandAndVerboseOption_WritesExpectedHelpText()
        {
            var rootCommand = new RootCommand("test");
            rootCommand.VerboseOption = rootCommand.AddOption('v', "verbose", "Enables verbose output.");
            var subcommand = rootCommand.AddSubcommand("delta", "Performs the delta operation.");
            subcommand.HelpVisibility = HelpVisibility.Verbose;

            string expectedText =
@"Usage: test [<options>]

Options:
  -v --verbose  Enables verbose output.

Specify ""-v"" for additional syntax.
";

            string expectedVerboseText =
@"Usage: test <command>
Usage: test [<options>]

Commands:
  delta  Performs the delta operation.

Options:
  -v --verbose  Enables verbose output.
";

            AssertHelpText(rootCommand, expectedText, expectedVerboseText);
        }

        [Fact]
        public static void WriteHelp_HasNeverVisibleSubcommand_WritesExpectedHelpText()
        {
            var rootCommand = new RootCommand("test");
            var subcommand = rootCommand.AddSubcommand("delta", "Performs the delta operation.");
            subcommand.HelpVisibility = HelpVisibility.Never;

            string expectedText =
@"Usage: test
";

            string expectedVerboseText = expectedText;

            AssertHelpText(rootCommand, expectedText, expectedVerboseText);
        }

        [Fact]
        public static void WriteHelp_HasAlwaysVisibleOption_WritesExpectedHelpText()
        {
            var rootCommand = new RootCommand("test");
            _ = rootCommand.AddOption('a', "alpha", "Enables the alpha subsystem.");

            string expectedText =
@"Usage: test [<options>]

Options:
  -a --alpha  Enables the alpha subsystem.
";

            string expectedVerboseText = expectedText;

            AssertHelpText(rootCommand, expectedText, expectedVerboseText);
        }

        [Fact]
        public static void WriteHelp_HasVerboseVisibleOption_WritesExpectedHelpText()
        {
            var rootCommand = new RootCommand("test");
            var option = rootCommand.AddOption('a', "alpha", "Enables the alpha subsystem.");
            option.HelpVisibility = HelpVisibility.Verbose;

            string expectedText =
@"Usage: test
";

            string expectedVerboseText =
@"Usage: test [<options>]

Options:
  -a --alpha  Enables the alpha subsystem.
";

            AssertHelpText(rootCommand, expectedText, expectedVerboseText);
        }

        [Fact]
        public static void WriteHelp_HasVerboseVisibleOptionAndVerboseOption_WritesExpectedHelpText()
        {
            var rootCommand = new RootCommand("test");
            rootCommand.VerboseOption = rootCommand.AddOption('v', "verbose", "Enables verbose output.");
            var option = rootCommand.AddOption('a', "alpha", "Enables the alpha subsystem.");
            option.HelpVisibility = HelpVisibility.Verbose;

            string expectedText =
@"Usage: test [<options>]

Options:
  -v --verbose  Enables verbose output.

Specify ""-v"" for additional syntax.
";

            string expectedVerboseText =
@"Usage: test [<options>]

Options:
  -v --verbose  Enables verbose output.
  -a --alpha    Enables the alpha subsystem.
";

            AssertHelpText(rootCommand, expectedText, expectedVerboseText);
        }

        [Fact]
        public static void WriteHelp_HasNeverVisibleOption_WritesExpectedHelpText()
        {
            var rootCommand = new RootCommand("test");
            var option = rootCommand.AddOption('a', "alpha", "Enables the alpha subsystem.");
            option.HelpVisibility = HelpVisibility.Never;

            string expectedText =
@"Usage: test
";

            string expectedVerboseText = expectedText;

            AssertHelpText(rootCommand, expectedText, expectedVerboseText);
        }

        [Fact]
        public static void WriteHelp_HasVisibleInheritedOption_WritesExpectedHelpText()
        {
            var rootCommand = new RootCommand("test");
            _ = rootCommand.AddOption('a', "alpha", "Enables the alpha subsystem.", inherited: true);
            var subcommand = rootCommand.AddSubcommand("delta", "Performs the delta operation.");

            string expectedText =
@"Usage: test delta [<options>]

Options:
  -a --alpha  Enables the alpha subsystem.
";

            string expectedVerboseText = expectedText;

            AssertHelpText(subcommand, expectedText, expectedVerboseText);
        }

        [Fact]
        public static void WriteHelp_HasVisibleShortOption_WritesExpectedHelpText()
        {
            var rootCommand = new RootCommand("test");
            _ = rootCommand.AddOption('a', null, "Enables the alpha subsystem.");

            string expectedText =
@"Usage: test [<options>]

Options:
  -a  Enables the alpha subsystem.
";

            string expectedVerboseText = expectedText;

            AssertHelpText(rootCommand, expectedText, expectedVerboseText);
        }

        [Fact]
        public static void WriteHelp_HasVisibleLongOption_WritesExpectedHelpText()
        {
            var rootCommand = new RootCommand("test");
            _ = rootCommand.AddOption(null, "alpha", "Enables the alpha subsystem.");

            string expectedText =
@"Usage: test [<options>]

Options:
     --alpha  Enables the alpha subsystem.
";

            string expectedVerboseText = expectedText;

            AssertHelpText(rootCommand, expectedText, expectedVerboseText);
        }

        [Fact]
        public static void WriteHelp_HasVisibleParameterizedOption_WritesExpectedHelpText()
        {
            var rootCommand = new RootCommand("test");
            _ = rootCommand.AddOption('a', "alpha", "value", "Enables the alpha subsystem.");

            string expectedText =
@"Usage: test [<options>]

Options:
  -a --alpha value  Enables the alpha subsystem.
";

            string expectedVerboseText = expectedText;

            AssertHelpText(rootCommand, expectedText, expectedVerboseText);
        }

        [Fact]
        public static void WriteHelp_HasVisibleOptionallyParameterizedOption_WritesExpectedHelpText()
        {
            var rootCommand = new RootCommand("test");
            var option = rootCommand.AddOption('a', "alpha", "value", "Enables the alpha subsystem.");
            option.SetOptionalParameterDefaultValue(string.Empty);

            string expectedText =
@"Usage: test [<options>]

Options:
  -a --alpha[=value]  Enables the alpha subsystem.
";

            string expectedVerboseText = expectedText;

            AssertHelpText(rootCommand, expectedText, expectedVerboseText);
        }

        [Fact]
        public static void WriteHelp_HasVisibleParameterizedVariadicOption_WritesExpectedHelpText()
        {
            var rootCommand = new RootCommand("test");
            _ = rootCommand.AddVariadicOption('a', "alpha", "value", "Enables the alpha subsystem.");

            string expectedText =
@"Usage: test [<options>]

Options:
  -a --alpha value ...  Enables the alpha subsystem.
";

            string expectedVerboseText = expectedText;

            AssertHelpText(rootCommand, expectedText, expectedVerboseText);
        }

        [Fact]
        public static void WriteHelp_HasVisibleOptionallyParameterizedVariadicOption_WritesExpectedHelpText()
        {
            var rootCommand = new RootCommand("test");
            var option = rootCommand.AddVariadicOption('a', "alpha", "value", "Enables the alpha subsystem.");
            option.SetOptionalParameterDefaultValue(string.Empty);

            string expectedText =
@"Usage: test [<options>]

Options:
  -a --alpha[=value] ...  Enables the alpha subsystem.
";

            string expectedVerboseText = expectedText;

            AssertHelpText(rootCommand, expectedText, expectedVerboseText);
        }

        [Fact]
        public static void WriteHelp_HasAlwaysVisibleRequiredOption_WritesExpectedHelpText()
        {
            var rootCommand = new RootCommand("test");
            var option = rootCommand.AddOption('a', "alpha", "Enables the alpha subsystem.");
            option.Required = true;

            string expectedText =
@"Usage: test -a

Options:
  -a --alpha  Enables the alpha subsystem.
";

            string expectedVerboseText = expectedText;

            AssertHelpText(rootCommand, expectedText, expectedVerboseText);
        }

        [Fact]
        public static void WriteHelp_HasVerboseVisibleRequiredOption_WritesExpectedHelpText()
        {
            var rootCommand = new RootCommand("test");
            var option = rootCommand.AddOption('a', "alpha", "Enables the alpha subsystem.");
            option.Required = true;
            option.HelpVisibility = HelpVisibility.Verbose;

            string expectedText =
@"Usage: test -a
";

            string expectedVerboseText =
@"Usage: test -a

Options:
  -a --alpha  Enables the alpha subsystem.
";

            AssertHelpText(rootCommand, expectedText, expectedVerboseText);
        }

        [Fact]
        public static void WriteHelp_HasVerboseVisibleRequiredOptionAndVerboseOption_WritesExpectedHelpText()
        {
            var rootCommand = new RootCommand("test");
            rootCommand.VerboseOption = rootCommand.AddOption('v', "verbose", "Enables verbose output.");
            var option = rootCommand.AddOption('a', "alpha", "Enables the alpha subsystem.");
            option.Required = true;
            option.HelpVisibility = HelpVisibility.Verbose;

            string expectedText =
@"Usage: test -a [<options>]

Options:
  -v --verbose  Enables verbose output.

Specify ""-v"" for additional syntax.
";

            string expectedVerboseText =
@"Usage: test -a [<options>]

Options:
  -v --verbose  Enables verbose output.
  -a --alpha    Enables the alpha subsystem.
";

            AssertHelpText(rootCommand, expectedText, expectedVerboseText);
        }

        [Fact]
        public static void WriteHelp_HasNeverVisibleRequiredOption_WritesExpectedHelpText()
        {
            var rootCommand = new RootCommand("test");
            var option = rootCommand.AddOption('a', "alpha", "Enables the alpha subsystem.");
            option.Required = true;
            option.HelpVisibility = HelpVisibility.Never;

            string expectedText =
@"Usage: test -a
";

            string expectedVerboseText = expectedText;

            AssertHelpText(rootCommand, expectedText, expectedVerboseText);
        }

        [Fact]
        public static void WriteHelp_HasVisibleInheritedRequiredOption_WritesExpectedHelpText()
        {
            var rootCommand = new RootCommand("test");
            var option = rootCommand.AddOption('a', "alpha", "Enables the alpha subsystem.", inherited: true);
            option.Required = true;
            var subcommand = rootCommand.AddSubcommand("delta", "Performs the delta operation.");

            string expectedText =
@"Usage: test delta -a

Options:
  -a --alpha  Enables the alpha subsystem.
";

            string expectedVerboseText = expectedText;

            AssertHelpText(subcommand, expectedText, expectedVerboseText);
        }

        [Fact]
        public static void WriteHelp_HasVisibleRequiredLongOption_WritesExpectedHelpText()
        {
            var rootCommand = new RootCommand("test");
            var option = rootCommand.AddOption(null, "alpha", "Enables the alpha subsystem.");
            option.Required = true;

            string expectedText =
@"Usage: test --alpha

Options:
     --alpha  Enables the alpha subsystem.
";

            string expectedVerboseText = expectedText;

            AssertHelpText(rootCommand, expectedText, expectedVerboseText);
        }

        [Fact]
        public static void WriteHelp_HasVisibleRequiredParameterizedOption_WritesExpectedHelpText()
        {
            var rootCommand = new RootCommand("test");
            var option = rootCommand.AddOption('a', "alpha", "value", "Enables the alpha subsystem.");
            option.Required = true;

            string expectedText =
@"Usage: test -a value

Options:
  -a --alpha value  Enables the alpha subsystem.
";

            string expectedVerboseText = expectedText;

            AssertHelpText(rootCommand, expectedText, expectedVerboseText);
        }

        [Fact]
        public static void WriteHelp_HasVisibleRequiredOptionallyParameterizedOption_WritesExpectedHelpText()
        {
            var rootCommand = new RootCommand("test");
            var option = rootCommand.AddOption('a', "alpha", "value", "Enables the alpha subsystem.");
            option.Required = true;
            option.SetOptionalParameterDefaultValue(string.Empty);

            string expectedText =
@"Usage: test -a[=value]

Options:
  -a --alpha[=value]  Enables the alpha subsystem.
";

            string expectedVerboseText = expectedText;

            AssertHelpText(rootCommand, expectedText, expectedVerboseText);
        }

        [Fact]
        public static void WriteHelp_HasVisibleRequiredParameterizedVariadicOption_WritesExpectedHelpText()
        {
            var rootCommand = new RootCommand("test");
            var option = rootCommand.AddVariadicOption('a', "alpha", "value", "Enables the alpha subsystem.");
            option.Required = true;

            string expectedText =
@"Usage: test -a value ...

Options:
  -a --alpha value ...  Enables the alpha subsystem.
";

            string expectedVerboseText = expectedText;

            AssertHelpText(rootCommand, expectedText, expectedVerboseText);
        }

        [Fact]
        public static void WriteHelp_HasVisibleRequiredOptionallyParameterizedVariadicOption_WritesExpectedHelpText()
        {
            var rootCommand = new RootCommand("test");
            var option = rootCommand.AddVariadicOption('a', "alpha", "value", "Enables the alpha subsystem.");
            option.Required = true;
            option.SetOptionalParameterDefaultValue(string.Empty);

            string expectedText =
@"Usage: test -a[=value] ...

Options:
  -a --alpha[=value] ...  Enables the alpha subsystem.
";

            string expectedVerboseText = expectedText;

            AssertHelpText(rootCommand, expectedText, expectedVerboseText);
        }

        [Fact]
        public static void WriteHelp_HasAlwaysVisibleParameter_WritesExpectedHelpText()
        {
            var rootCommand = new RootCommand("test");
            _ = rootCommand.AddParameter("sierra", "The sierra value.");

            string expectedText =
@"Usage: test sierra

Parameters:
  sierra  The sierra value.
";

            string expectedVerboseText =
@"Usage: test [--] sierra

Parameters:
  sierra  The sierra value.
";

            AssertHelpText(rootCommand, expectedText, expectedVerboseText);
        }

        [Fact]
        public static void WriteHelp_HasVerboseVisibleParameter_WritesExpectedHelpText()
        {
            var rootCommand = new RootCommand("test");
            var parameter = rootCommand.AddParameter("sierra", "The sierra value.");
            parameter.HelpVisibility = HelpVisibility.Verbose;

            string expectedText =
@"Usage: test sierra
";

            string expectedVerboseText =
@"Usage: test [--] sierra

Parameters:
  sierra  The sierra value.
";

            AssertHelpText(rootCommand, expectedText, expectedVerboseText);
        }

        [Fact]
        public static void WriteHelp_HasVerboseVisibleParameterAndVerboseOption_WritesExpectedHelpText()
        {
            var rootCommand = new RootCommand("test");
            rootCommand.VerboseOption = rootCommand.AddOption('v', "verbose", "Enables verbose output.");
            var parameter = rootCommand.AddParameter("sierra", "The sierra value.");
            parameter.HelpVisibility = HelpVisibility.Verbose;

            string expectedText =
@"Usage: test [<options>] sierra

Options:
  -v --verbose  Enables verbose output.

Specify ""-v"" for additional syntax.
";

            string expectedVerboseText =
@"Usage: test [<options>] [--] sierra

Parameters:
  sierra  The sierra value.

Options:
  -v --verbose  Enables verbose output.
";

            AssertHelpText(rootCommand, expectedText, expectedVerboseText);
        }

        [Fact]
        public static void WriteHelp_HasNeverVisibleParameter_WritesExpectedHelpText()
        {
            var rootCommand = new RootCommand("test");
            var parameter = rootCommand.AddParameter("sierra", "The sierra value.");
            parameter.HelpVisibility = HelpVisibility.Never;

            string expectedText =
@"Usage: test sierra
";

            string expectedVerboseText =
@"Usage: test [--] sierra
";

            AssertHelpText(rootCommand, expectedText, expectedVerboseText);
        }

        [Fact]
        public static void WriteHelp_HasVisibleVariadicParameter_WritesExpectedHelpText()
        {
            var rootCommand = new RootCommand("test");
            _ = rootCommand.AddVariadicParameter("sierra", "The sierra value.");

            string expectedText =
@"Usage: test sierra ...

Parameters:
  sierra  The sierra value.
";

            string expectedVerboseText =
@"Usage: test [--] sierra ...

Parameters:
  sierra  The sierra value.
";

            AssertHelpText(rootCommand, expectedText, expectedVerboseText);
        }

        [Fact]
        public static void WriteHelp_HasAlwaysVisibleOptionalParameter_WritesExpectedHelpText()
        {
            var rootCommand = new RootCommand("test");
            var parameter = rootCommand.AddParameter("sierra", "The sierra value.");
            parameter.Optional = true;

            string expectedText =
@"Usage: test [sierra]

Parameters:
  sierra  The sierra value.
";

            string expectedVerboseText =
@"Usage: test [--] [sierra]

Parameters:
  sierra  The sierra value.
";

            AssertHelpText(rootCommand, expectedText, expectedVerboseText);
        }

        [Fact]
        public static void WriteHelp_HasVerboseVisibleOptionalParameter_WritesExpectedHelpText()
        {
            var rootCommand = new RootCommand("test");
            var parameter = rootCommand.AddParameter("sierra", "The sierra value.");
            parameter.Optional = true;
            parameter.HelpVisibility = HelpVisibility.Verbose;

            string expectedText =
@"Usage: test [sierra]
";

            string expectedVerboseText =
@"Usage: test [--] [sierra]

Parameters:
  sierra  The sierra value.
";

            AssertHelpText(rootCommand, expectedText, expectedVerboseText);
        }

        [Fact]
        public static void WriteHelp_HasVerboseVisibleOptionalParameterAndVerboseOption_WritesExpectedHelpText()
        {
            var rootCommand = new RootCommand("test");
            rootCommand.VerboseOption = rootCommand.AddOption('v', "verbose", "Enables verbose output.");
            var parameter = rootCommand.AddParameter("sierra", "The sierra value.");
            parameter.Optional = true;
            parameter.HelpVisibility = HelpVisibility.Verbose;

            string expectedText =
@"Usage: test [<options>] [sierra]

Options:
  -v --verbose  Enables verbose output.

Specify ""-v"" for additional syntax.
";

            string expectedVerboseText =
@"Usage: test [<options>] [--] [sierra]

Parameters:
  sierra  The sierra value.

Options:
  -v --verbose  Enables verbose output.
";

            AssertHelpText(rootCommand, expectedText, expectedVerboseText);
        }

        [Fact]
        public static void WriteHelp_HasNeverVisibleOptionalParameter_WritesExpectedHelpText()
        {
            var rootCommand = new RootCommand("test");
            var parameter = rootCommand.AddParameter("sierra", "The sierra value.");
            parameter.Optional = true;
            parameter.HelpVisibility = HelpVisibility.Never;

            string expectedText =
@"Usage: test [sierra]
";

            string expectedVerboseText =
@"Usage: test [--] [sierra]
";

            AssertHelpText(rootCommand, expectedText, expectedVerboseText);
        }

        [Fact]
        public static void WriteHelp_HasVisibleVariadicOptionalParameter_WritesExpectedHelpText()
        {
            var rootCommand = new RootCommand("test");
            var parameter = rootCommand.AddVariadicParameter("sierra", "The sierra value.");
            parameter.Optional = true;

            string expectedText =
@"Usage: test [sierra ...]

Parameters:
  sierra  The sierra value.
";

            string expectedVerboseText =
@"Usage: test [--] [sierra ...]

Parameters:
  sierra  The sierra value.
";

            AssertHelpText(rootCommand, expectedText, expectedVerboseText);
        }

        [Fact]
        public static void WriteHelp_HasRequiredParameterAfterOptionalParameter_WritesExpectedHelpText()
        {
            var rootCommand = new RootCommand("test");
            var parameter = rootCommand.AddParameter("sierra", "The sierra value.");
            parameter.Optional = true;
            _ = rootCommand.AddParameter("tango", "The tango value.");

            string expectedText =
@"Usage: test [sierra tango]

Parameters:
  sierra  The sierra value.
  tango   The tango value.
";

            string expectedVerboseText =
@"Usage: test [--] [sierra tango]

Parameters:
  sierra  The sierra value.
  tango   The tango value.
";

            AssertHelpText(rootCommand, expectedText, expectedVerboseText);
        }

        [Fact]
        public static void WriteHelp_HasOptionalParameterAfterOptionalParameter_WritesExpectedHelpText()
        {
            var rootCommand = new RootCommand("test");
            var parameterS = rootCommand.AddParameter("sierra", "The sierra value.");
            parameterS.Optional = true;
            var parameterT = rootCommand.AddParameter("tango", "The tango value.");
            parameterT.Optional = true;

            string expectedText =
@"Usage: test [sierra [tango]]

Parameters:
  sierra  The sierra value.
  tango   The tango value.
";

            string expectedVerboseText =
@"Usage: test [--] [sierra [tango]]

Parameters:
  sierra  The sierra value.
  tango   The tango value.
";

            AssertHelpText(rootCommand, expectedText, expectedVerboseText);
        }

        [Fact]
        public static void WriteHelp_HasOneOfEach_WritesExpectedHelpText()
        {
            var rootCommand = new RootCommand("test");
            _ = rootCommand.AddSubcommand("delta", "Performs the delta operation.");
            _ = rootCommand.AddParameter("sierra", "The sierra value.");
            _ = rootCommand.AddOption('a', "alpha", "Enables the alpha subsystem.");

            string expectedText =
@"Usage: test <command>
Usage: test [<options>] sierra

Commands:
  delta  Performs the delta operation.

Parameters:
  sierra  The sierra value.

Options:
  -a --alpha  Enables the alpha subsystem.
";

            string expectedVerboseText =
@"Usage: test <command>
Usage: test [<options>] [--] sierra

Commands:
  delta  Performs the delta operation.

Parameters:
  sierra  The sierra value.

Options:
  -a --alpha  Enables the alpha subsystem.
";

            AssertHelpText(rootCommand, expectedText, expectedVerboseText);
        }

        [Fact]
        public static void WriteHelp_InheritedVerboseOption_WritesExpectedHelpText()
        {
            var rootCommand = new RootCommand("test");
            rootCommand.VerboseOption = rootCommand.AddOption('v', "verbose", "Enables verbose output.", inherited: true);
            rootCommand.VerboseOption.HelpVisibility = HelpVisibility.Verbose;
            var command = rootCommand.AddSubcommand("delta", "");

            string expectedText =
@"Usage: test delta

Specify ""-v"" for additional syntax.
";

            string expectedVerboseText =
@"Usage: test delta [<options>]

Options:
  -v --verbose  Enables verbose output.
";

            AssertHelpText(command, expectedText, expectedVerboseText);
        }

        [Fact]
        public static void WriteHelp_ShortVerboseOption_WritesExpectedHelpText()
        {
            var rootCommand = new RootCommand("test");
            rootCommand.VerboseOption = rootCommand.AddOption('v', null, "Enables verbose output.");
            rootCommand.VerboseOption.HelpVisibility = HelpVisibility.Verbose;

            string expectedText =
@"Usage: test

Specify ""-v"" for additional syntax.
";

            string expectedVerboseText =
@"Usage: test [<options>]

Options:
  -v  Enables verbose output.
";

            AssertHelpText(rootCommand, expectedText, expectedVerboseText);
        }

        [Fact]
        public static void WriteHelp_LongVerboseOption_WritesExpectedHelpText()
        {
            var rootCommand = new RootCommand("test");
            rootCommand.VerboseOption = rootCommand.AddOption(null, "verbose", "Enables verbose output.");
            rootCommand.VerboseOption.HelpVisibility = HelpVisibility.Verbose;

            string expectedText =
@"Usage: test

Specify ""--verbose"" for additional syntax.
";

            string expectedVerboseText =
@"Usage: test [<options>]

Options:
     --verbose  Enables verbose output.
";

            AssertHelpText(rootCommand, expectedText, expectedVerboseText);
        }

        [Fact]
        public static void WriteHelp_OverflowLeftColumn_WritesExpectedHelpText()
        {
            var rootCommand = new RootCommand("test");
            _ = rootCommand.AddOption(null, "alpha", "Enables the alpha subsystem.");
            _ = rootCommand.AddOption(null, "alpha-alpha-alpha-alpha", "Enables the alpha subsystem.");

            string expectedText =
@"Usage: test [<options>]

Options:
     --alpha  Enables the alpha subsystem.
     --alpha-alpha-alpha-alpha
              Enables the alpha subsystem.
";

            string expectedVerboseText = expectedText;

            AssertHelpText(rootCommand, expectedText, expectedVerboseText);
        }

        [Fact]
        public static void WriteHelp_OverflowLeftColumnCusp_WritesExpectedHelpText()
        {
            var rootCommand = new RootCommand("test");
            _ = rootCommand.AddOption(null, "alpha-alpha-alpha-a", "Enables the alpha subsystem.");
            _ = rootCommand.AddOption(null, "alpha-alpha-alpha-al", "Enables the alpha subsystem.");

            string expectedText =
@"Usage: test [<options>]

Options:
     --alpha-alpha-alpha-a  Enables the alpha subsystem.
     --alpha-alpha-alpha-al
                            Enables the alpha subsystem.
";

            string expectedVerboseText = expectedText;

            AssertHelpText(rootCommand, expectedText, expectedVerboseText);
        }

        [Fact]
        public static void WriteHelp_OverflowRightColumn_WritesExpectedHelpText()
        {
            var rootCommand = new RootCommand("test");
            _ = rootCommand.AddOption(null, "alpha-alpha-alpha", "Enables the alpha subsystem.  Enables the alpha subsystem.  Enables the alpha subsystem.  Enables the alpha subsystem.");
            _ = rootCommand.AddOption(null, "bravo", "Enables the bravo subsystem.");

            string expectedText =
@"Usage: test [<options>]

Options:
     --alpha-alpha-alpha  Enables the alpha subsystem.  Enables the alpha
                          subsystem.  Enables the alpha subsystem.  Enables the
                          alpha subsystem.
     --bravo              Enables the bravo subsystem.
";

            string expectedVerboseText = expectedText;

            AssertHelpText(rootCommand, expectedText, expectedVerboseText);
        }

        [Fact]
        public static void WriteHelp_OverflowRightColumnNoWordBreaks_WritesExpectedHelpText()
        {
            var rootCommand = new RootCommand("test");
            _ = rootCommand.AddOption(null, "alpha", "01234567890123456789012345678901234567890123456789012");
            _ = rootCommand.AddOption(null, "alpha-alpha", "012345678901234567890123456789012345678901234567890123");
            _ = rootCommand.AddOption(null, "alpha-alpha-alpha", "012345678901234567890123456789012345678901234567890123456789");
            _ = rootCommand.AddOption(null, "bravo", "Enables the bravo subsystem.");

            string expectedText =
@"Usage: test [<options>]

Options:
     --alpha              01234567890123456789012345678901234567890123456789012
     --alpha-alpha        01234567890123456789012345678901234567890123456789012
                          3
     --alpha-alpha-alpha  01234567890123456789012345678901234567890123456789012
                          3456789
     --bravo              Enables the bravo subsystem.
";

            string expectedVerboseText = expectedText;

            AssertHelpText(rootCommand, expectedText, expectedVerboseText);
        }

        [Theory]
        [InlineData("\n")]
        [InlineData("\r\n")]
        public static void WriteHelp_OverflowRightColumnLineBreak_WritesExpectedHelpText(string lineBreak)
        {
            var rootCommand = new RootCommand("test");
            _ = rootCommand.AddOption(null, "alpha", $"012345678901234567890123456789{lineBreak}012345678901234567890123456789");
            _ = rootCommand.AddOption(null, "alpha-alpha", $"01234567890123456789012345678901234567890123456789012{lineBreak}3");
            _ = rootCommand.AddOption(null, "alpha-alpha-alpha", $"012345678901234567890123456789012345678901234567890123{lineBreak}4");
            _ = rootCommand.AddOption(null, "bravo", "Enables the bravo subsystem.");

            string expectedText =
@"Usage: test [<options>]

Options:
     --alpha              012345678901234567890123456789
                          012345678901234567890123456789
     --alpha-alpha        01234567890123456789012345678901234567890123456789012
                          3
     --alpha-alpha-alpha  01234567890123456789012345678901234567890123456789012
                          3
                          4
     --bravo              Enables the bravo subsystem.
";

            string expectedVerboseText = expectedText;

            AssertHelpText(rootCommand, expectedText, expectedVerboseText);
        }

        private static void AssertHelpText(Command command, string expectedText, string expectedVerboseText)
        {
            expectedText = expectedText.Replace("\r\n", "\n");
            expectedVerboseText = expectedVerboseText.Replace("\r\n", "\n");

            var writer = new StringWriter();
            CommandHelp.WriteHelp(command, writer, verbose: false);
            string helpText = writer.ToString().Replace(Environment.NewLine, "\n");

            Assert.Equal(expectedText, helpText);

            writer = new StringWriter();
            CommandHelp.WriteHelp(command, writer, verbose: true);
            string verboseHelpText = writer.ToString().Replace(Environment.NewLine, "\n");

            Assert.Equal(expectedVerboseText, verboseHelpText);
        }

        [Fact]
        public static void WriteHelpHint_CommandIsNull_ThrowsArgumentNullException()
        {
            var rootCommand = new RootCommand("test");

            var ex = Assert.Throws<ArgumentNullException>(() => CommandHelp.WriteHelpHint(null!, new StringWriter()));

            Assert.Equal("command", ex.ParamName);
        }

        [Fact]
        public static void WriteHelpHint_WriterIsNull_ThrowsArgumentNullException()
        {
            var rootCommand = new RootCommand("test");

            var ex = Assert.Throws<ArgumentNullException>(() => CommandHelp.WriteHelpHint(rootCommand, null!));

            Assert.Equal("writer", ex.ParamName);
        }

        [Fact]
        public static void WriteHelpHint_NoHelpOption_WritesExpectedHelpText()
        {
            var rootCommand = new RootCommand("test");

            string expectedText = @"";

            AssertHelpHintText(rootCommand, expectedText);
        }

        [Fact]
        public static void WriteHelpHint_HelpOption_WritesExpectedHelpText()
        {
            var rootCommand = new RootCommand("test");
            rootCommand.HelpOption = rootCommand.AddOption('h', "help", "Shows help.");

            string expectedText =
@"Try ""test -h"" for more information.
";

            AssertHelpHintText(rootCommand, expectedText);
        }

        [Fact]
        public static void WriteHelpHint_InheritedHelpOption_WritesExpectedHelpText()
        {
            var rootCommand = new RootCommand("test");
            rootCommand.HelpOption = rootCommand.AddOption('h', "help", "Shows help.", inherited: true);
            var subcommand = rootCommand.AddSubcommand("delta", "");

            string expectedText =
@"Try ""test delta -h"" for more information.
";

            AssertHelpHintText(subcommand, expectedText);
        }

        [Fact]
        public static void WriteHelpHint_ShortHelpOption_WritesExpectedHelpText()
        {
            var rootCommand = new RootCommand("test");
            rootCommand.HelpOption = rootCommand.AddOption('h', null, "Shows help.");

            string expectedText =
@"Try ""test -h"" for more information.
";

            AssertHelpHintText(rootCommand, expectedText);
        }

        [Fact]
        public static void WriteHelpHint_LongHelpOption_WritesExpectedHelpText()
        {
            var rootCommand = new RootCommand("test");
            rootCommand.HelpOption = rootCommand.AddOption(null, "help", "Shows help.");

            string expectedText =
@"Try ""test --help"" for more information.
";

            AssertHelpHintText(rootCommand, expectedText);
        }

        private static void AssertHelpHintText(Command command, string expectedText)
        {
            expectedText = expectedText.Replace("\r\n", "\n");

            var writer = new StringWriter();
            CommandHelp.WriteHelpHint(command, writer);
            string helpText = writer.ToString().Replace(Environment.NewLine, "\n");

            Assert.Equal(expectedText, helpText);
        }
    }
}
