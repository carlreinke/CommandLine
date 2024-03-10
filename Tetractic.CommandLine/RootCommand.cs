// Copyright 2024 Carl Reinke
//
// This file is part of a library that is licensed under the terms of the GNU
// Lesser General Public License Version 3 as published by the Free Software
// Foundation.
//
// This license does not grant rights under trademark law for use of any trade
// names, trademarks, or service marks.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.InteropServices;

namespace Tetractic.CommandLine
{
    /// <summary>
    /// Represents the initial command of a command line application.
    /// </summary>
    public sealed class RootCommand : Command
    {
        private const string _optionsTerminatorDescription = "Causes subsequent arguments to not be interpreted as options.";

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
        /// Gets the completions for a specified command-line argument.
        /// </summary>
        /// <param name="args">The command-line arguments.</param>
        /// <param name="index">The index of the element in <paramref name="args"/> to complete.
        ///     </param>
        /// <returns>The completions for the specified argument.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="args"/> is
        ///     <see langword="null"/>.</exception>
        /// <exception cref="ArgumentNullException">An element of <paramref name="args"/> is
        ///     <see langword="null"/>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is less than zero
        ///     or is greater than or equal to the length of <paramref name="args"/>.</exception>
        public List<Completion> GetCompletions(string[] args, int index)
        {
            if (args is null)
                throw new ArgumentNullException(nameof(args));
            if (index < 0 || index >= args.Length)
                throw new ArgumentOutOfRangeException(nameof(index));

            var completions = new List<Completion>();

            var parser = new ArgParser(this);
            IEnumerator<CommandParameter>? parameters = null;

            for (int i = 0; ; ++i)
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
                        if (parser.HasPendingShortOptions)
                        {
                            result = parser.ParseShortOption();
                            goto next;
                        }

                        if (i == index)
                        {
                            // No completions.
                            return completions;
                        }
                        break;
                    }
                    case ArgParser.ResultKind.ShortOption:
                    {
                        var option = result.Option!;
                        string? value = result.Value;

                        if (option is ParameterizedCommandOption parameterizedOption)
                        {
                            if (value is null && parameterizedOption.ParameterIsOptional)
                            {
                                if (i == index && !parser.HasPendingShortOptions)
                                {
                                    if (option.HelpVisibility != HelpVisibility.Never)
                                    {
                                        completions.Add(new Completion(arg, option.Description));
                                        completions.Add(new Completion(arg + "=", option.Description));
                                    }

                                    return completions;
                                }
                            }
                            else
                            {
                                if (value is null)
                                {
                                    if (parser.HasPendingShortOptions)
                                    {
                                        result = parser.ParseShortOption();
                                        goto next;
                                    }

                                    if (i == index)
                                    {
                                        if (option.HelpVisibility != HelpVisibility.Never)
                                            completions.Add(new Completion(arg + "=", option.Description));

                                        return completions;
                                    }

                                    if (i + 1 == index)
                                    {
                                        AddOptionParameterCompletions(completions, parameterizedOption, args[index]);

                                        return completions;
                                    }

                                    i += 1;
                                }
                                else
                                {
                                    if (i == index)
                                    {
                                        Debug.Assert(!parser.HasPendingShortOptions);

                                        AddOptionParameterCompletionsWithValue(completions, parameterizedOption, arg, value);

                                        return completions;
                                    }
                                }
                            }
                        }
                        else
                        {
                            if (i == index && !parser.HasPendingShortOptions)
                            {
                                if (value is null && option.HelpVisibility != HelpVisibility.Never)
                                    completions.Add(new Completion(arg, option.Description));

                                return completions;
                            }
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
                        if (i == index)
                        {
                            AddOptionLongNameCompletions(completions, parser.CommandOptions, result.LongName!);

                            return completions;
                        }
                        break;
                    }
                    case ArgParser.ResultKind.LongOption:
                    {
                        var option = result.Option!;
                        string? value = result.Value;

                        if (option is ParameterizedCommandOption parameterizedOption)
                        {
                            if (value is null && parameterizedOption.ParameterIsOptional)
                            {
                                if (i == index)
                                {
                                    AddOptionLongNameCompletions(completions, parser.CommandOptions, result.LongName!);

                                    return completions;
                                }
                            }
                            else
                            {
                                if (value is null)
                                {
                                    if (i == index)
                                    {
                                        AddOptionLongNameCompletions(completions, parser.CommandOptions, result.LongName!);

                                        return completions;
                                    }

                                    if (i + 1 == index)
                                    {
                                        AddOptionParameterCompletions(completions, parameterizedOption, args[index]);

                                        return completions;
                                    }

                                    i += 1;
                                }
                                else
                                {
                                    if (i == index)
                                    {
                                        AddOptionParameterCompletionsWithValue(completions, parameterizedOption, arg, value);

                                        return completions;
                                    }
                                }
                            }
                        }
                        else
                        {
                            if (i == index)
                            {
                                if (value is null)
                                    AddOptionLongNameCompletions(completions, parser.CommandOptions, result.LongName!);

                                return completions;
                            }
                        }
                        break;
                    }
                    case ArgParser.ResultKind.OptionsTerminator:
                    {
                        if (i == index)
                        {
                            completions.Add(new Completion(arg, _optionsTerminatorDescription));

                            AddOptionLongNameCompletions(completions, parser.CommandOptions, "");

                            return completions;
                        }
                        break;
                    }
                    case ArgParser.ResultKind.Subcommand:
                    {
                        if (i == index)
                        {
                            var parentCommand = parser.Command.Parent!;

                            AddSubcommandCompletions(completions, parentCommand.Subcommands, arg);

                            var parentParameters = parentCommand.Parameters;
                            if (parentParameters.Count > 0)
                                AddParameterCompletions(completions, parentParameters[0], arg, parser.OptionsTerminated);

                            return completions;
                        }
                        break;
                    }
                    case ArgParser.ResultKind.Parameter:
                    {
                        if (parameters is null)
                        {
                            if (i == index)
                            {
                                AddSubcommandCompletions(completions, parser.Command.Subcommands, arg);

                                if (arg == "-" && !parser.OptionsTerminated)
                                    AddOptionCompletions(completions, parser.CommandOptions);
                            }

                            parameters = parser.Command.Parameters.GetEnumerator();
                            if (!parameters.MoveNext())
                            {
                                if (i == index)
                                    return completions;

                                parameters = SpeculatedParameterEnumerator.Instance;
                            }
                        }
                        else
                        {
                            if (i == index)
                            {
                                if (arg == "-" && !parser.OptionsTerminated)
                                    AddOptionCompletions(completions, parser.CommandOptions);
                            }

                            if (!parameters.Current.Variadic && !parameters.MoveNext())
                            {
                                if (i == index)
                                    return completions;

                                parameters = SpeculatedParameterEnumerator.Instance;
                            }
                        }

                        var parameter = parameters.Current;

                        if (i == index)
                        {
                            if (arg != "-" || parser.OptionsTerminated)
                                AddParameterCompletions(completions, parameter, arg, parser.OptionsTerminated);

                            return completions;
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

            static void AddOptionCompletions(List<Completion> completions, List<CommandOption> options)
            {
                completions.Add(new Completion("--", _optionsTerminatorDescription));

                foreach (var option in options)
                {
                    if (option.HelpVisibility == HelpVisibility.Never)
                        continue;

                    if (option.LongName is null)
                    {
                        if (option is ParameterizedCommandOption parameterizedOption)
                        {
                            if (parameterizedOption.ParameterIsOptional)
                                completions.Add(new Completion($"-{option.ShortName}", option.Description));

                            completions.Add(new Completion($"-{option.ShortName}=", option.Description));
                        }
                        else
                        {
                            completions.Add(new Completion($"-{option.ShortName}", option.Description));
                        }
                    }
                    else
                    {
                        if (option is ParameterizedCommandOption parameterizedOption)
                        {
                            if (parameterizedOption.ParameterIsOptional)
                                completions.Add(new Completion($"--{option.LongName}", option.Description));

                            completions.Add(new Completion($"--{option.LongName}=", option.Description));
                        }
                        else
                        {
                            completions.Add(new Completion($"--{option.LongName}", option.Description));
                        }
                    }
                }
            }

            static void AddOptionLongNameCompletions(List<Completion> completions, List<CommandOption> options, string name)
            {
                foreach (var option in options)
                {
                    if (option.HelpVisibility == HelpVisibility.Never)
                        continue;

                    if (option.LongName is null)
                        continue;

                    if (!option.LongName.StartsWith(name, StringComparison.InvariantCulture))
                        continue;

                    if (option is ParameterizedCommandOption parameterizedOption)
                    {
                        if (parameterizedOption.ParameterIsOptional)
                            completions.Add(new Completion("--" + option.LongName, option.Description));

                        completions.Add(new Completion("--" + option.LongName + "=", option.Description));
                    }
                    else
                    {
                        completions.Add(new Completion("--" + option.LongName, option.Description));
                    }
                }
            }

            static void AddOptionParameterCompletions(List<Completion> completions, ParameterizedCommandOption parameterizedOption, string text)
            {
                var parameterCompletionProvider = parameterizedOption.ParameterCompletionProvider;
                var parameterCompletions = parameterCompletionProvider?.GetCompletions(text);
                if (parameterCompletions != null)
                    completions.AddRange(parameterCompletions);
            }

            static void AddOptionParameterCompletionsWithValue(List<Completion> completions, ParameterizedCommandOption parameterizedOption, string text, string value)
            {
                var parameterCompletionProvider = parameterizedOption.ParameterCompletionProvider;
                var parameterCompletions = parameterCompletionProvider?.GetCompletions(value);
                if (parameterCompletions != null)
                {
                    string prefix = text.Substring(0, text.Length - value.Length);
                    foreach (var completion in parameterCompletions)
                        completions.Add(new Completion(prefix + completion.Text, completion.Description));
                }
            }

            static void AddSubcommandCompletions(List<Completion> completions, CommandList subcommands, string name)
            {
                foreach (var command in subcommands)
                {
                    if (command.HelpVisibility == HelpVisibility.Never)
                        continue;

                    if (!command.Name.StartsWith(name, StringComparison.InvariantCulture))
                        continue;

                    completions.Add(new Completion(command.Name, command.Description));
                }
            }

            static void AddParameterCompletions(List<Completion> completions, CommandParameter parameter, string text, bool optionsTerminated)
            {
                var parameterCompletionProvider = parameter.CompletionProvider;
                var parameterCompletions = parameterCompletionProvider?.GetCompletions(text);
                if (parameterCompletions != null)
                {
                    if (optionsTerminated)
                    {
                        completions.AddRange(parameterCompletions);
                        return;
                    }

                    bool hasOptionAmbiguity = false;
                    int optionsTerminatorIndex = completions.Count;

                    foreach (var completion in parameterCompletions)
                    {
                        if (completion.Text.Length > 0 && completion.Text[0] == '-')
                            hasOptionAmbiguity = true;
                        else
                            completions.Add(completion);
                    }

                    if (hasOptionAmbiguity && text.Length == 0)
                        completions.Insert(optionsTerminatorIndex, new Completion("--", _optionsTerminatorDescription));
                }
            }
        }

        /// <summary>
        /// Resets the parameters and options so that the instance can be reused.
        /// </summary>
        public new void Reset() => base.Reset();

        private sealed class SpeculatedParameterEnumerator : IEnumerator<CommandParameter>
        {
            public static readonly SpeculatedParameterEnumerator Instance = new SpeculatedParameterEnumerator();

            private SpeculatedParameterEnumerator()
            {
            }

            public CommandParameter Current { get; } = new VariadicCommandParameter<object>("", "", TryParse);

            [ExcludeFromCodeCoverage]
            object IEnumerator.Current => Current;

            [ExcludeFromCodeCoverage]
            public void Dispose()
            {
            }

            [ExcludeFromCodeCoverage]
            public bool MoveNext() => false;

            [ExcludeFromCodeCoverage]
            public void Reset()
            {
            }

            [ExcludeFromCodeCoverage]
            private static bool TryParse(string text, [MaybeNullWhen(false)] out object value)
            {
                value = null;
                return false;
            }
        }
    }
}
