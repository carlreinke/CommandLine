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

namespace Tetractic.CommandLine
{
    /// <summary>
    /// Provides help text for commands.
    /// </summary>
    public static class CommandHelp
    {
        /// <summary>
        /// Writes help text for a command.
        /// </summary>
        /// <param name="command">The command.</param>
        /// <param name="writer">The destination for written text.</param>
        /// <param name="verbose">Controls whether the help text includes entities with
        ///     <see cref="HelpVisibility.Verbose"/> visibility.</param>
        /// <param name="maxWidth">The number of columns after which a line should wrap.  The
        ///     minimum value is 80.</param>
        /// <exception cref="ArgumentNullException"><paramref name="command"/> is
        ///     <see langword="null"/>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="writer"/> is
        ///     <see langword="null"/>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="maxWidth"/> is less than
        ///     80.</exception>
        /// <exception cref="IOException">An I/O error occurs when writing to
        ///     <paramref name="writer"/>.</exception>
        public static void WriteHelp(Command command, TextWriter writer, bool verbose, int maxWidth = 80)
        {
            if (command is null)
                throw new ArgumentNullException(nameof(command));
            if (writer is null)
                throw new ArgumentNullException(nameof(writer));
            if (maxWidth < 80)
                throw new ArgumentOutOfRangeException(nameof(maxWidth));

            var visibilityLimit = verbose ? HelpVisibility.Verbose : HelpVisibility.Always;

            var subcommands = command.Subcommands;
            bool hasVisibleCommands = subcommands.Exists(c => IsVisible(c.HelpVisibility));
            var options = command.GetOptions();
            bool hasRequiredOptions = options.Exists(o => o.Required);
            bool hasVisibleOptions = options.Exists(o => IsVisible(o.HelpVisibility));
            bool hasVisibleOptionalOptions = options.Exists(o => IsVisible(o.HelpVisibility) && !o.Required);
            var parameters = command.Parameters;
            bool hasParameters = parameters.Count > 0;
            bool hasVisibleParameters = parameters.Exists(a => IsVisible(a.HelpVisibility));

            string commandPath = command.GetCommandPath();

            if (hasVisibleCommands)
                writer.WriteLine($"Usage: {commandPath} <command>");

            if (hasRequiredOptions || hasVisibleOptions || hasParameters || !hasVisibleCommands)
            {
                writer.Write($"Usage: {commandPath}");

                foreach (var option in options)
                {
                    if (!option.Required)
                        continue;

                    if (option.ShortName is char shortName)
                    {
                        writer.Write(" -");
                        writer.Write(shortName);
                    }
                    else
                    {
                        writer.Write(" --");
                        writer.Write(option.LongName);
                    }
                    if (option is ParameterizedCommandOption parameterizedOption)
                    {
                        if (parameterizedOption.ParameterIsOptional)
                        {
                            writer.Write("[=");
                            writer.Write(parameterizedOption.ParameterName);
                            writer.Write(']');
                        }
                        else
                        {
                            writer.Write(' ');
                            writer.Write(parameterizedOption.ParameterName);
                        }
                    }
                    if (option.Variadic)
                        writer.Write(" ...");
                }

                if (hasVisibleOptionalOptions)
                    writer.Write(" [<options>]");

                if (hasParameters && verbose)
                    writer.Write(" [--]");

                int optionalArgumentDepth = 0;
                foreach (var parameter in parameters)
                {
                    writer.Write(' ');
                    if (parameter.Optional)
                    {
                        writer.Write('[');
                        optionalArgumentDepth += 1;
                    }
                    writer.Write(parameter.Name);
                    if (parameter.Variadic)
                        writer.Write(" ...");
                }
                for (int i = 0; i < optionalArgumentDepth; ++i)
                    writer.Write(']');

                writer.WriteLine();
            }

            if (hasVisibleCommands)
            {
                writer.WriteLine();
                writer.WriteLine("Commands:");

                var leftColumnInfo = new LeftColumnInfo(maxWidth);

                foreach (var subcommand in subcommands)
                {
                    if (!IsVisible(subcommand.HelpVisibility))
                        continue;

                    leftColumnInfo.AdjustLength(subcommand.Name);
                }

                var twoColumnWriter = new TwoColumnWriter(writer, leftColumnInfo.Length, maxWidth);

                foreach (var subcommand in subcommands)
                {
                    if (!IsVisible(subcommand.HelpVisibility))
                        continue;

                    twoColumnWriter.Write(subcommand.Name, subcommand.Description);
                }
            }

            if (hasVisibleParameters)
            {
                writer.WriteLine();
                writer.WriteLine("Parameters:");

                var leftColumnInfo = new LeftColumnInfo(maxWidth);

                foreach (var parameter in parameters)
                {
                    if (!IsVisible(parameter.HelpVisibility))
                        continue;

                    leftColumnInfo.AdjustLength(parameter.Name);
                }

                var twoColumnWriter = new TwoColumnWriter(writer, leftColumnInfo.Length, maxWidth);

                foreach (var parameter in parameters)
                {
                    if (!IsVisible(parameter.HelpVisibility))
                        continue;

                    twoColumnWriter.Write(parameter.Name, parameter.Description);
                }
            }

            if (hasVisibleOptions)
            {
                writer.WriteLine();
                writer.WriteLine("Options:");

                var leftColumnInfo = new LeftColumnInfo(maxWidth);

                string[] optionSyntaxes = new string[options.Count];

                for (int i = 0; i < options.Count; ++i)
                {
                    var option = options[i];

                    if (!IsVisible(option.HelpVisibility))
                        continue;

                    string syntax = GetOptionSyntax(option);

                    optionSyntaxes[i] = syntax;

                    leftColumnInfo.AdjustLength(syntax);
                }

                var twoColumnWriter = new TwoColumnWriter(writer, leftColumnInfo.Length, maxWidth);

                for (int i = 0; i < options.Count; ++i)
                {
                    var option = options[i];

                    if (!IsVisible(option.HelpVisibility))
                        continue;

                    twoColumnWriter.Write(optionSyntaxes[i], option.Description);
                }
            }

            if (!verbose)
            {
                var verboseOption = command.GetVerboseOption();
                if (verboseOption != null &&
                    (subcommands.Exists(c => c.HelpVisibility == HelpVisibility.Verbose) ||
                     parameters.Exists(a => a.HelpVisibility == HelpVisibility.Verbose) ||
                     options.Exists(o => o.HelpVisibility == HelpVisibility.Verbose)))
                {
                    writer.WriteLine();
                    writer.Write(@"Specify """);
                    if (verboseOption.ShortName is char shortName)
                    {
                        writer.Write('-');
                        writer.Write(shortName);
                    }
                    else
                    {
                        writer.Write("--");
                        writer.Write(verboseOption.LongName);
                    }
                    writer.WriteLine(@""" for additional syntax.");
                }
            }

            static string GetOptionSyntax(CommandOption option)
            {
                string result;
                if (option.ShortName is char shortName)
                    result = $"-{shortName}";
                else
                    result = "  ";
                if (option.LongName != null)
                    result += $" --{option.LongName}";
                if (option is ParameterizedCommandOption parameterizedOption)
                    result += parameterizedOption.ParameterIsOptional
                        ? $"[={parameterizedOption.ParameterName}]"
                        : $" {parameterizedOption.ParameterName}";
                if (option.Variadic)
                    result += " ...";
                return result;
            }

            bool IsVisible(HelpVisibility visibility) => visibility <= visibilityLimit;

        }

        /// <summary>
        /// Writes a hint that suggests specifying the help option for a command if the command has
        /// a help option.
        /// </summary>
        /// <param name="command">The command.</param>
        /// <param name="writer">The destination for written text.</param>
        /// <exception cref="ArgumentNullException"><paramref name="command"/> is
        ///     <see langword="null"/>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="writer"/> is
        ///     <see langword="null"/>.</exception>
        /// <exception cref="IOException">An I/O error occurs when writing to
        ///     <paramref name="writer"/>.</exception>
        public static void WriteHelpHint(Command command, TextWriter writer)
        {
            if (command is null)
                throw new ArgumentNullException(nameof(command));
            if (writer is null)
                throw new ArgumentNullException(nameof(writer));

            var helpOption = command.GetHelpOption();
            if (helpOption is null)
                return;

            writer.Write(@"Try """);
            writer.Write(command.GetCommandPath());
            writer.Write(' ');
            if (helpOption.ShortName is char shortName)
            {
                writer.Write('-');
                writer.Write(shortName);
            }
            else
            {
                writer.Write("--");
                writer.Write(helpOption.LongName);
            }
            writer.WriteLine(@""" for more information.");
        }

        private struct LeftColumnInfo
        {
            private readonly int _maxLength;
            private int _length;

            public LeftColumnInfo(int maxWidth)
            {
                _maxLength = maxWidth / 3 - 2;
                _length = 0;
            }

            public int Length => _length;

            public void AdjustLength(string text)
            {
                int length = text.Length;
                if (length <= _maxLength)
                    _length = Math.Max(length, _length);
            }
        }

        private readonly struct TwoColumnWriter
        {
            private readonly TextWriter _writer;
            private readonly int _leftLength;
            private readonly char[] _spaces;
            private readonly int _rightMaxLength;

            public TwoColumnWriter(TextWriter writer, int leftLength, int maxWidth)
            {
                _writer = writer;
                _leftLength = leftLength;
                _spaces = new char[leftLength + 2 + 2];
                for (int i = 0; i < _spaces.Length; ++i)
                    _spaces[i] = ' ';
                // Right max length is less one because writing to the last column moves the cursor to the next row.
                _rightMaxLength = maxWidth - _spaces.Length - 1;
            }

            /// <exception cref="IOException">An I/O error occurs.</exception>
            // ExceptionAdjustment: M:System.IO.TextWriter.Write(System.String,System.Object) -T:System.FormatException
            public void Write(string leftText, string rightText)
            {
                // Left column (and spaces).
                if (leftText.Length > _leftLength)
                {
                    _writer.Write("  ");
                    _writer.WriteLine(leftText);
                    _writer.Write(_spaces);
                }
                else
                {
                    _writer.Write("  ");
                    _writer.Write(leftText);
                    _writer.Write(_spaces, 0, _leftLength - leftText.Length + 2);
                }

                // Right column.
                for (var wordWrapEnumerator = new WordWrapLineEnumerator(rightText, _rightMaxLength); ; )
                {
                    (string line, bool isLast) = wordWrapEnumerator.TakeNext();

                    _writer.WriteLine(line);

                    if (isLast)
                        break;

                    _writer.Write(_spaces);
                }
            }
        }

        private struct WordWrapLineEnumerator
        {
            private readonly string _text;

            private readonly int _lineMaxLength;

            private int _lineStart;

            public WordWrapLineEnumerator(string text, int lineMaxLength)
            {
                _text = text;
                _lineMaxLength = lineMaxLength;
                _lineStart = 0;
            }

            public (string Line, bool IsLast) TakeNext()
            {
                int lineEnd = _lineStart;
                int nextLineStart = _lineStart;

                bool wasSpace = false;

                for (int i = _lineStart; ; ++i)
                {
                    int nextLineSkip;

                    // Line ends at end.
                    if (i == _text.Length)
                    {
                        nextLineSkip = 0;
                        goto lineEnd;
                    }

                    // Line ends at line break.
                    if (_text[i] == '\n')
                    {
                        nextLineSkip = 1;
                        goto lineEnd;
                    }
                    else if (i + 1 < _text.Length && _text[i] == '\r' && _text[i + 1] == '\n')
                    {
                        nextLineSkip = 2;
                        goto lineEnd;
                    }

                    bool isSpace = _text[i] == ' ';

                    // Word break when transitioning from space to non-space.
                    if (wasSpace && !isSpace)
                    {
                        // If line exceeded max length then use previous word break.
                        if (i - 1 - _lineStart > _lineMaxLength)
                            break;

                        lineEnd = i - 1;
                        nextLineStart = i;
                    }

                    wasSpace = isSpace;
                    continue;

                lineEnd:
                    // If line exceeded max length then use previous word break.
                    if (i - _lineStart > _lineMaxLength)
                        break;

                    lineEnd = i;
                    nextLineStart = i + nextLineSkip;
                    break;
                }

                // Force word break if necessary.
                if (nextLineStart == _lineStart && _lineStart != _text.Length)
                {
                    lineEnd = _lineStart + _lineMaxLength;
                    nextLineStart = lineEnd;
                }

                string line = _text.Substring(_lineStart, lineEnd - _lineStart);

                _lineStart = nextLineStart;

                return (line, nextLineStart == _text.Length);
            }
        }
    }
}
