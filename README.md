# CommandLine

[![Build status](https://ci.appveyor.com/api/projects/status/4n204x18tonnh06w/branch/master?svg=true)](https://ci.appveyor.com/project/carlreinke/commandline/branch/master) [![Test coverage](https://codecov.io/gh/Tetractic/CommandLine/branch/master/graph/badge.svg)](https://codecov.io/gh/Tetractic/CommandLine) [![NuGet package](https://img.shields.io/nuget/vpre/Tetractic.CommandLine?logo=nuget)](https://www.nuget.org/packages/Tetractic.CommandLine/)

This library provides command-line argument parsing and help text generation.

## Features

 * Strongly-typed argument parsing
 * Help text generation
 * Subcommands
 * Parameters
   * Optional or required
   * Monadic (single value) or variadic (list of values)
 * Options
   * Optional or required
   * Monadic (single value) or variadic (list of values)
   * Short (`-f`) or long (`--foo`)
   * Bundling (`-abc` is the same as `-a -b -c`)
   * Parameterization:
     * Unparameterized (`-f` or `--foo`)
     * Parameterized (`-f bar` or `--foo bar` or `-f=bar` or `--foo=bar`)
     * Optionally parameterized (`-f` or `--foo` or `-f=bar` or `--foo=bar`)

## Example Application

The following code is a simple application that takes a list of numbers and outputs a filtered list.

```C#
using System;
using Tetractic.CommandLine;

internal static class Program
{
    internal static int Main(string[] args)
    {
        var rootCommand = new RootCommand("filter");
        {
            var numParameter = rootCommand.AddVariadicParameter<int>(
                name: "NUM",
                description: "The integers to filter.",
                parse: int.TryParse);
            numParameter.Optional = true;

            var minOption = rootCommand.AddOption<int>(
                shortName: null,
                longName: "min",
                parameterName: "NUM",
                description: "The minimum integer to include.",
                parse: int.TryParse);

            var maxOption = rootCommand.AddOption<int>(
                shortName: null,
                longName: "max",
                parameterName: "NUM",
                description: "The maximum integer to include.",
                parse: int.TryParse);

            rootCommand.HelpOption = rootCommand.AddOption(
                shortName: 'h',
                longName: "help",
                description: "Shows help.");

            rootCommand.SetInvokeHandler(() =>
            {
                var min = minOption.GetValueOrDefault(int.MinValue);
                var max = maxOption.GetValueOrDefault(int.MaxValue);

                foreach (int num in numParameter.Values)
                    if (num >= min && num <= max)
                        Console.WriteLine(num);
                return 0;
            });
        }

        try
        {
            return rootCommand.Execute(args);
        }
        catch (InvalidCommandLineException ex)
        {
            Console.Error.WriteLine(ex.Message);
            CommandHelp.WriteHelpHint(ex.Command, Console.Error);
            return -1;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex.Message);
            return -1;
        }
    }
}
```

When run with an invalid argument, it displays an error message:
```console
$ filter --min 0 zero
Invalid argument "zero".
Try "filter -h" for more information.
```

When run with the `-h` argument, it displays the following help text:
```console
$ filter -h
Usage: filter [<options>] [NUM ...]

Parameters:
  NUM  The integers to filter.

Options:
     --min NUM  The minimum integer to include.
     --max NUM  The maximum integer to include.
  -h --help     Shows help.
```

When run with an valid arguments, it executes the command:
```console
$ filter --min 3 --max 9 1 2 3 5 8 13 21
3
5
8
```
