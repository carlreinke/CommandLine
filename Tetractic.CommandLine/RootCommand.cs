// Copyright 2024 Carl Reinke
//
// This file is part of a library that is licensed under the terms of the GNU
// Lesser General Public License Version 3 as published by the Free Software
// Foundation.
//
// This license does not grant rights under trademark law for use of any trade
// names, trademarks, or service marks.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace Tetractic.CommandLine
{
    /// <summary>
    /// Represents the initial command of a command line application.
    /// </summary>
    public sealed class RootCommand : Command
    {
        /// <summary>
        /// Initializes a new <see cref="RootCommand"/>.
        /// </summary>
        /// <param name="name">The name of the executable.</param>
        /// <exception cref="ArgumentNullException"><paramref name="name"/> is
        ///     <see langword="null"/>.</exception>
        public RootCommand(string name)
            : base(null, name, string.Empty)
        {
        }

        /// <summary>
        /// Executes the root command with a specified help text output destination and specified
        /// command-line arguments.
        /// </summary>
        /// <param name="args">The command-line arguments.</param>
        /// <returns>The return code of the command that was invoked.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="args"/> is
        ///     <see langword="null"/>.</exception>
        /// <exception cref="ArgumentNullException">An element of <paramref name="args"/> is
        ///     <see langword="null"/>.</exception>
        /// <exception cref="InvalidCommandLineException">The arguments were invalid.</exception>
        /// <exception cref="IOException">An I/O error occurs when writing help text.</exception>
        /// <remarks>
        /// <para>
        /// Help text is written if a help option is specified or if the first parameter of the
        /// specified command is not optional and does not have a value.
        /// </para>
        /// <para>
        /// The default help handler writes the help text to <see cref="Console.Error"/>.
        /// </para>
        /// </remarks>
        public int Execute(string[] args)
        {
            if (args is null)
                throw new ArgumentNullException(nameof(args));

            var parser = new ArgParser(this);
            IEnumerator<CommandParameter>? parameters = null;

            for (int i = 0; i < args.Length; ++i)
            {
                string arg = args[i];

                if (arg is null)
                    throw new ArgumentNullException(nameof(args), "Array element cannot be null.");

                var result = parser.Parse(arg);
            next:
                switch (result.Kind)
                {
                    case ArgParser.ResultKind.UnrecognizedShortOption:
                    {
                        throw new InvalidCommandLineException(parser.Command, @$"Unrecognized option ""-{result.ShortName}"".");
                    }
                    case ArgParser.ResultKind.ShortOption:
                    {
                        var option = result.Option!;
                        string? value = result.Value;

                        if (option is ParameterizedCommandOption parameterizedOption)
                        {
                            if (value is null && parameterizedOption.ParameterIsOptional)
                            {
                                option.Accept();
                            }
                            else
                            {
                                if (value is null)
                                {
                                    if (parser.HasPendingShortOptions || i + 1 == args.Length)
                                        throw new InvalidCommandLineException(parser.Command, @$"Missing value for option ""-{result.ShortName}"".");

                                    i += 1;
                                    value = args[i];
                                }

                                if (!option.TryAcceptValue(value))
                                    throw new InvalidCommandLineException(parser.Command, @$"Invalid value ""{value}"" for option ""-{result.ShortName}"".");
                            }
                        }
                        else
                        {
                            if (value != null)
                                throw new InvalidCommandLineException(parser.Command, @$"Unexpected value ""{value}"" for option ""-{result.ShortName}"".");

                            option.Accept();
                        }

                        if (parser.HasPendingShortOptions)
                        {
                            result = parser.ParseShortOption();
                            goto next;
                        }
                        break;
                    }
                    case ArgParser.ResultKind.UnrecognizedLongOption:
                    {
                        throw new InvalidCommandLineException(parser.Command, @$"Unrecognized option ""--{result.LongName}"".");
                    }
                    case ArgParser.ResultKind.LongOption:
                    {
                        var option = result.Option!;
                        string? value = result.Value;

                        if (option is ParameterizedCommandOption parameterizedOption)
                        {
                            if (value is null && parameterizedOption.ParameterIsOptional)
                            {
                                option.Accept();
                            }
                            else
                            {
                                if (value is null)
                                {
                                    if (i + 1 == args.Length)
                                        throw new InvalidCommandLineException(parser.Command, @$"Missing value for option ""--{result.LongName}"".");

                                    i += 1;
                                    value = args[i];
                                }

                                if (!option.TryAcceptValue(value))
                                    throw new InvalidCommandLineException(parser.Command, @$"Invalid value ""{value}"" for option ""--{result.LongName}"".");
                            }
                        }
                        else
                        {
                            if (value != null)
                                throw new InvalidCommandLineException(parser.Command, @$"Unexpected value ""{value}"" for option ""--{result.LongName}"".");

                            option.Accept();
                        }
                        break;
                    }
                    case ArgParser.ResultKind.OptionsTerminator:
                    case ArgParser.ResultKind.Subcommand:
                    {
                        break;
                    }
                    case ArgParser.ResultKind.Parameter:
                    {
                        if (parameters is null)
                        {
                            parameters = parser.Command.Parameters.GetEnumerator();
                            if (!parameters.MoveNext())
                                throw new InvalidCommandLineException(parser.Command, @$"Unexpected argument ""{arg}"".");
                        }
                        else
                        {
                            if (!parameters.Current.Variadic && !parameters.MoveNext())
                                throw new InvalidCommandLineException(parser.Command, @$"Unexpected argument ""{arg}"".");
                        }

                        var parameter = parameters.Current;

                        if (parameter.ExpandWildcardsOnWindows &&
                            RuntimeInformation.IsOSPlatform(OSPlatform.Windows) &&
                            WindowsWildcardExpander.ContainsAnyWildcard(arg))
                        {
                            foreach (string expandedArg in WindowsWildcardExpander.EnumerateMatches(arg))
                                if (!parameter.TryAcceptValue(expandedArg))
                                    throw new InvalidCommandLineException(parser.Command, @$"Invalid argument ""{expandedArg}"".");
                        }
                        else
                        {
                            if (!parameter.TryAcceptValue(arg))
                                throw new InvalidCommandLineException(parser.Command, @$"Invalid argument ""{arg}"".");
                        }
                        break;
                    }
                    default:
                    {
                        Debug.Fail("Unreachable.");
                        break;
                    }
                }
            }

            var command = parser.Command;
            var commandOptions = parser.CommandOptions;
            var helpOption = command.GetHelpOption();
            var verboseOption = command.GetVerboseOption();

            if (helpOption?.Count > 0)
            {
                command.WriteHelp(verboseOption?.Count > 0);

                return 0;
            }

            foreach (var option in commandOptions)
            {
                if (option.Required && option.Count == 0)
                {
                    string optionNameSyntax = option.LongName is string longName ? $"--{longName}" : $"-{option.ShortName}";
                    throw new InvalidCommandLineException(command, @$"Missing required option ""{optionNameSyntax}"".");
                }
            }

            foreach (var parameter in command.Parameters)
            {
                if (parameter.Optional && parameter.Count == 0)
                    break;

                if (parameter.Count == 0)
                {
                    if (parameters != null || HasAnyOptionExcept(commandOptions, verboseOption))
                        throw new InvalidCommandLineException(command, $"Expected additional arguments.");

                    command.WriteHelp(verboseOption?.Count > 0);

                    return -1;
                }
            }

            return command.Invoke();

            static bool HasAnyOptionExcept(List<CommandOption> options, CommandOption? excludedOption)
            {
                foreach (var option in options)
                    if (option.Count > 0 && option != excludedOption)
                        return true;

                return false;
            }
        }

        /// <summary>
        /// Resets the parameters and options so that the instance can be reused.
        /// </summary>
        public new void Reset() => base.Reset();
    }
}
