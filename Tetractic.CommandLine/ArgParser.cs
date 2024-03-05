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

namespace Tetractic.CommandLine
{
    internal struct ArgParser
    {
        internal Command Command;
        internal List<CommandOption> CommandOptions;
        internal bool OptionsTerminated;
        internal bool CommandDetermined;

        private string? _shortOptions;
        private int _shortOptionIndex;

        internal ArgParser(RootCommand rootCommand)
        {
            Command = rootCommand;
            CommandOptions = rootCommand.GetOptions();
            OptionsTerminated = false;
            CommandDetermined = false;

            _shortOptions = null;
            _shortOptionIndex = 0;
        }

        internal readonly bool HasPendingShortOptions => _shortOptions != null;

        internal Result Parse(string arg)
        {
            Debug.Assert(_shortOptions is null);

            if (!OptionsTerminated && arg.Length > 1 && arg[0] == '-')
            {
                if (arg[1] != '-')
                {
                    // Handle short option(s).
                    _shortOptions = arg;
                    _shortOptionIndex = 1;

                    return ParseShortOption();
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

                    var option = FindOptionLong(CommandOptions, name);
                    if (option is null)
                        return new Result(ResultKind.UnrecognizedLongOption, '\0', name);

                    CommandDetermined |= !option.Inherited;

                    return new Result(ResultKind.LongOption, '\0', name, option, value);
                }
                else
                {
                    // "--" terminates option parsing.
                    OptionsTerminated = true;

                    return new Result(ResultKind.OptionsTerminator);
                }
            }

            if (!CommandDetermined)
            {
                foreach (var subcommand in Command.Subcommands)
                {
                    if (arg.Equals(subcommand.Name, StringComparison.InvariantCulture))
                    {
                        Command = subcommand;
                        CommandOptions = Command.GetOptions();

                        return new Result(ResultKind.Subcommand);
                    }
                }

                CommandDetermined = true;
            }

            return new Result(ResultKind.Parameter);
        }

        internal Result ParseShortOption()
        {
            string arg = _shortOptions!;
            int index = _shortOptionIndex;

            char name = arg[index];
            string? value;
            index += 1;

            if (index != arg.Length && arg[index] == '=')
            {
                value = arg.Substring(index + 1);

                _shortOptions = null;
            }
            else
            {
                value = null;

                if (index != arg.Length)
                    _shortOptionIndex = index;
                else
                    _shortOptions = null;
            }

            var option = FindOptionShort(CommandOptions, name);
            if (option is null)
                return new Result(ResultKind.UnrecognizedShortOption, name);

            CommandDetermined |= !option.Inherited;

            return new Result(ResultKind.ShortOption, name, null, option, value);
        }

        private static CommandOption? FindOptionShort(List<CommandOption> options, char name)
        {
            foreach (var option in options)
                if (option.ShortName is char shortName && name.Equals(shortName))
                    return option;

            return null;
        }

        private static CommandOption? FindOptionLong(List<CommandOption> options, string name)
        {
            foreach (var option in options)
                if (name.Equals(option.LongName, StringComparison.InvariantCulture))
                    return option;

            return null;
        }

        internal readonly struct Result
        {
            internal readonly ResultKind Kind;
            internal readonly char ShortName;
            internal readonly string? LongName;
            internal readonly CommandOption? Option;
            internal readonly string? Value;

            internal Result(ResultKind resultKind, char shortName = '\0', string? longName = null, CommandOption? option = null, string? value = null)
            {
                Kind = resultKind;
                ShortName = shortName;
                LongName = longName;
                Option = option;
                Value = value;
            }
        }

        internal enum ResultKind
        {
            UnrecognizedShortOption,
            ShortOption,
            UnrecognizedLongOption,
            LongOption,
            OptionsTerminator,
            Subcommand,
            Parameter,
        }
    }
}
