// Copyright 2022 Carl Reinke
//
// This file is part of a library that is licensed under the terms of GNU Lesser
// General Public License version 3 as published by the Free Software
// Foundation.
//
// This license does not grant rights under trademark law for use of any trade
// names, trademarks, or service marks.

using System;
using System.Collections.Generic;
using System.IO;

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

            Command command = this;
            var helpOption = command.HelpOption;
            var verboseOption = command.VerboseOption;
            var commandOptions = command.GetOptions();

            bool optionsTerminated = false;
            bool commandDetermined = false;
            IEnumerator<CommandParameter>? parameters = null;

            for (int a = 0; a < args.Length; ++a)
            {
                string arg = args[a];

                if (arg is null)
                    throw new ArgumentNullException(nameof(args), "Array element cannot be null.");

                if (!optionsTerminated && arg.Length > 1 && arg[0] == '-')
                {
                    if (arg[1] != '-')
                    {
                        // Handle short option(s).
                        for (int o = 1; o < arg.Length; ++o)
                        {
                            char name = arg[o];

                            var option = FindOptionShort(commandOptions, name);
                            if (option is null)
                                throw new InvalidCommandLineException(command, @$"Unrecognized option ""-{name}"".");

                            if (option is ParameterizedCommandOption parameterizedOption)
                            {
                                string? value;

                                if (o + 1 != arg.Length && arg[o + 1] == '=')
                                {
                                    value = arg.Substring(o + 2);
                                    o = arg.Length - 1;
                                }
                                else
                                {
                                    value = null;
                                }

                                if (value is null && parameterizedOption.ParameterIsOptional)
                                {
                                    option.Accept();
                                }
                                else
                                {
                                    if (value is null)
                                    {
                                        if (a + 1 == args.Length)
                                            throw new InvalidCommandLineException(command, @$"Missing value for option ""-{name}"".");

                                        a += 1;
                                        value = args[a];
                                    }

                                    if (!option.TryAcceptValue(value))
                                        throw new InvalidCommandLineException(command, @$"Invalid value ""{value}"" for option ""-{name}"".");
                                }
                            }
                            else
                            {
                                option.Accept();
                            }

                            commandDetermined |= !option.Inherited;
                        }
                        continue;
                    }
                    else if (arg.Length > 2)
                    {
                        // Handle long option.
                        string name;
                        string? value;

                        int separatorIndex = arg.IndexOf('=', 2);
                        if (separatorIndex >= 0)
                        {
                            name = arg.Substring(2, separatorIndex - 2);
                            value = arg.Substring(separatorIndex + 1);
                        }
                        else
                        {
                            name = arg.Substring(2);
                            value = null;
                        }

                        var option = FindOptionLong(commandOptions, name);
                        if (option is null)
                            throw new InvalidCommandLineException(command, @$"Unrecognized option ""--{name}"".");

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
                                    if (a + 1 == args.Length)
                                        throw new InvalidCommandLineException(command, @$"Missing value for option ""--{name}"".");

                                    a += 1;
                                    value = args[a];
                                }

                                if (!option.TryAcceptValue(value))
                                    throw new InvalidCommandLineException(command, @$"Invalid value ""{value}"" for option ""--{name}"".");
                            }
                        }
                        else
                        {
                            if (value != null)
                                throw new InvalidCommandLineException(command, @$"Unexpected value ""{value}"" for option ""--{name}"".");

                            option.Accept();
                        }

                        commandDetermined |= !option.Inherited;
                        continue;
                    }
                    else
                    {
                        // "--" terminates option parsing.
                        optionsTerminated = true;
                        continue;
                    }
                }

                if (!commandDetermined)
                {
                    foreach (var subcommand in command.Subcommands)
                    {
                        if (arg.Equals(subcommand.Name, StringComparison.InvariantCulture))
                        {
                            command = subcommand;
                            helpOption = command.GetHelpOption();
                            verboseOption = command.GetVerboseOption();
                            commandOptions = command.GetOptions();
                            goto nextArg;
                        }
                    }

                    commandDetermined = true;
                }

                if (parameters is null)
                {
                    parameters = command.Parameters.GetEnumerator();
                    if (!parameters.MoveNext())
                        throw new InvalidCommandLineException(command, @$"Unexpected argument ""{arg}"".");
                }
                else
                {
                    if (!parameters.Current.Variadic && !parameters.MoveNext())
                        throw new InvalidCommandLineException(command, @$"Unexpected argument ""{arg}"".");
                }

                if (!parameters.Current.TryAcceptValue(arg))
                    throw new InvalidCommandLineException(command, @$"Invalid argument ""{arg}"".");

            nextArg:
                ;
            }

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
                    if (parameters != null)
                        throw new InvalidCommandLineException(command, $"Expected additional arguments.");

                    command.WriteHelp(verboseOption?.Count > 0);

                    return -1;
                }
            }

            return command.Invoke();

            static CommandOption? FindOptionShort(List<CommandOption> options, char name)
            {
                foreach (var option in options)
                    if (name.Equals(option.ShortName))
                        return option;

                return null;
            }

            static CommandOption? FindOptionLong(List<CommandOption> options, string name)
            {
                foreach (var option in options)
                    if (name.Equals(option.LongName, StringComparison.InvariantCulture))
                        return option;

                return null;
            }
        }
    }
}
