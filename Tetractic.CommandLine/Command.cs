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
using System.IO;
using System.Threading.Tasks;

namespace Tetractic.CommandLine
{
    /// <summary>
    /// Represents a command.
    /// </summary>
    public partial class Command
    {
        private readonly List<Command> _subcommands = new List<Command>();

        private readonly List<CommandParameter> _parameters = new List<CommandParameter>();

        private readonly List<CommandOption> _options = new List<CommandOption>();

        private CommandOption? _helpOption;

        private CommandOption? _verboseOption;

        private Func<int> _invoke;

        private HelpHandler _writeHelp;

        /// <exception cref="ArgumentNullException"><paramref name="name"/> is
        ///     <see langword="null"/>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="description"/> is
        ///     <see langword="null"/>.</exception>
        internal Command(Command? parent, string name, string description)
        {
            if (name is null)
                throw new ArgumentNullException(nameof(name));
            if (description is null)
                throw new ArgumentNullException(nameof(description));

            Parent = parent;
            Name = name;
            Description = description;
            _invoke = DefaultInvoke;
            _writeHelp = DefaultWriteHelp;
        }

        /// <summary>
        /// Gets the parent command of the command, if any.
        /// </summary>
        /// <value>The parent command of the command, if any.</value>
        public Command? Parent { get; }

        /// <summary>
        /// Gets the name of the command.
        /// </summary>
        /// <value>The name of the command.</value>
        public string Name { get; }

        /// <summary>
        /// Gets the description of the command.
        /// </summary>
        /// <value>The description of the command.</value>
        public string Description { get; }

        /// <summary>
        /// Gets or sets a value indicating when the command appears in the help text for the parent
        /// command.
        /// </summary>
        /// <value>A value indicating when the command appears in the help text for the parent
        ///     command.</value>
        public HelpVisibility HelpVisibility { get; set; }

        /// <summary>
        /// Gets or sets the option that causes help text output rather than command invocation when
        /// specified.
        /// </summary>
        /// <value>The option that causes help text output rather than command invocation when
        ///     specified.</value>
        /// <exception cref="InvalidOperationException" accessor="set">The command option does not
        ///     exist on the command.</exception>
        public CommandOption? HelpOption
        {
            get => _helpOption;
            set
            {
                if (value != null && !_options.Contains(value))
                    throw new InvalidOperationException("The command option does not exist on the command.");

                _helpOption = value;
            }
        }

        /// <summary>
        /// Gets or sets the option that causes help text output to be verbose when specified.
        /// </summary>
        /// <value>The option that causes help text output to be verbose when specified.</value>
        /// <exception cref="InvalidOperationException" accessor="set">The command option does not
        ///     exist on the command.</exception>
        public CommandOption? VerboseOption
        {
            get => _verboseOption;
            set
            {
                if (value != null && !_options.Contains(value))
                    throw new InvalidOperationException("The command option does not exist on the command.");

                _verboseOption = value;
            }
        }

        /// <summary>
        /// Gets a read-only list of subcommands added to the command.
        /// </summary>
        /// <value>A read-only list of subcommands added to the command.</value>
        public CommandList Subcommands => new CommandList(_subcommands);

        /// <summary>
        /// Gets a read-only list of parameters added to the command.
        /// </summary>
        /// <value>A read-only list of parameters added to the command.</value>
        public ParameterList Parameters => new ParameterList(_parameters);

        /// <summary>
        /// Gets a read-only list of options added to the command.
        /// </summary>
        /// <value>A read-only list of options added to the command.</value>
        public OptionList Options => new OptionList(_options);

        /// <summary>
        /// Adds a subcommand to the command.
        /// </summary>
        /// <param name="name">The name of the subcommand.</param>
        /// <param name="description">The description of the subcommand.</param>
        /// <returns>The subcommand that was added.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="name"/> is
        ///     <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="name"/> is invalid.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="description"/> is
        ///     <see langword="null"/>.</exception>
        /// <exception cref="InvalidOperationException">The command already has a subcommand with
        ///     the same name.</exception>
        public Command AddSubcommand(string name, string description)
        {
            if (name is null)
                throw new ArgumentNullException(nameof(name));
            if (name.Length == 0 || name[0] == '-' || ContainsWhiteSpace(name))
                throw new ArgumentException("Invalid name.", nameof(name));

            var command = new Command(this, name, description);

            foreach (var existingSubcommand in _subcommands)
                if (command.Name.Equals(existingSubcommand.Name, StringComparison.InvariantCulture))
                    throw new InvalidOperationException("The command already has a subcommand with the same name.");

            _subcommands.Add(command);

            return command;
        }

        /// <summary>
        /// Adds a parameter to the command.
        /// </summary>
        /// <param name="name">The name of the parameter.</param>
        /// <param name="description">The description of the parameter.</param>
        /// <returns>The parameter that was added.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="name"/> is
        ///     <see langword="null"/>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="description"/> is
        ///     <see langword="null"/>.</exception>
        /// <exception cref="InvalidOperationException">The command has a variadic parameter.
        ///     </exception>
        /// <exception cref="InvalidOperationException">The command already has a parameter with the
        ///     same name.</exception>
        public CommandParameter<string> AddParameter(string name, string description)
        {
            return AddParameter<string>(name, description, ParseString);
        }

        /// <summary>
        /// Adds a parameter with a specified value parser to the command.
        /// </summary>
        /// <param name="name">The name of the parameter.</param>
        /// <param name="description">The description of the parameter.</param>
        /// <param name="parse">A delegate that attempts to parse a value for the parameter from
        ///     specified text.</param>
        /// <returns>The parameter that was added.</returns>
        /// <typeparam name="T">The type of value that the command parameter stores.</typeparam>
        /// <exception cref="ArgumentNullException"><paramref name="name"/> is
        ///     <see langword="null"/>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="description"/> is
        ///     <see langword="null"/>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="parse"/> is
        ///     <see langword="null"/>.</exception>
        /// <exception cref="InvalidOperationException">The command has a variadic parameter.
        ///     </exception>
        /// <exception cref="InvalidOperationException">The command already has a parameter with the
        ///     same name.</exception>
        public CommandParameter<T> AddParameter<T>(string name, string description, TryParser<T> parse)
        {
            ValidateParameterName(name);

            var parameter = new CommandParameter<T>(name, description, parse);

            return AddParameter(parameter);
        }

        /// <summary>
        /// Adds a parameter to the command.  The parameter can store multiple values.
        /// </summary>
        /// <param name="name">The name of the parameter.</param>
        /// <param name="description">The description of the parameter.</param>
        /// <returns>The parameter that was added.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="name"/> is
        ///     <see langword="null"/>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="description"/> is
        ///     <see langword="null"/>.</exception>
        /// <exception cref="InvalidOperationException">The command has a variadic parameter.
        ///     </exception>
        /// <exception cref="InvalidOperationException">The command already has a parameter with the
        ///     same name.</exception>
        public VariadicCommandParameter<string> AddVariadicParameter(string name, string description)
        {
            return AddVariadicParameter<string>(name, description, ParseString);
        }

        /// <summary>
        /// Adds a parameter with a specified value parser to the command.  The parameter can store
        /// multiple values.
        /// </summary>
        /// <param name="name">The name of the parameter.</param>
        /// <param name="description">The description of the parameter.</param>
        /// <param name="parse">A delegate that attempts to parse a value for the parameter from
        ///     specified text.</param>
        /// <returns>The parameter that was added.</returns>
        /// <typeparam name="T">The type of values that the command parameter stores.</typeparam>
        /// <exception cref="ArgumentNullException"><paramref name="name"/> is
        ///     <see langword="null"/>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="description"/> is
        ///     <see langword="null"/>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="parse"/> is
        ///     <see langword="null"/>.</exception>
        /// <exception cref="InvalidOperationException">The command has a variadic parameter.
        ///     </exception>
        /// <exception cref="InvalidOperationException">The command already has a parameter with the
        ///     same name.</exception>
        public VariadicCommandParameter<T> AddVariadicParameter<T>(string name, string description, TryParser<T> parse)
        {
            ValidateParameterName(name);

            var parameter = new VariadicCommandParameter<T>(name, description, parse);

            return AddParameter(parameter);
        }

        /// <summary>
        /// Adds a parameterless option to the command.
        /// </summary>
        /// <param name="shortName">The short name of the option.</param>
        /// <param name="longName">The long name of the option.</param>
        /// <param name="description">The description of the option.</param>
        /// <param name="inherited">Controls whether the command option is inherited by subcommands.
        ///     </param>
        /// <returns>The option that was added.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="shortName"/> is
        ///     <see langword="null"/> and <paramref name="longName"/> is <see langword="null"/>.
        ///     </exception>
        /// <exception cref="ArgumentException"><paramref name="shortName"/> is invalid.</exception>
        /// <exception cref="ArgumentException"><paramref name="longName"/> is invalid.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="description"/> is
        ///     <see langword="null"/>.</exception>
        /// <exception cref="InvalidOperationException">The command already has an option with the
        ///     same short name.</exception>
        /// <exception cref="InvalidOperationException">The command already has an option with the
        ///     same long name.</exception>
        public CommandOption AddOption(char? shortName, string? longName, string description, bool inherited = false)
        {
            ValidateOptionNames(shortName, longName);

            var option = new CommandOption(shortName, longName, description, inherited);

            return AddOption(option);
        }

        /// <summary>
        /// Adds a parameterized option to the command.
        /// </summary>
        /// <param name="shortName">The short name of the option.</param>
        /// <param name="longName">The long name of the option.</param>
        /// <param name="parameterName">The name of the parameter of the option.</param>
        /// <param name="description">The description of the option.</param>
        /// <param name="inherited">Controls whether the command option is inherited by subcommands.
        ///     </param>
        /// <returns>The option that was added.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="shortName"/> is
        ///     <see langword="null"/> and <paramref name="longName"/> is <see langword="null"/>.
        ///     </exception>
        /// <exception cref="ArgumentException"><paramref name="shortName"/> is invalid.</exception>
        /// <exception cref="ArgumentException"><paramref name="longName"/> is invalid.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="parameterName"/> is
        ///     <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="parameterName"/> is invalid.
        ///     </exception>
        /// <exception cref="ArgumentNullException"><paramref name="description"/> is
        ///     <see langword="null"/>.</exception>
        /// <exception cref="InvalidOperationException">The command already has an option with the
        ///     same short name.</exception>
        /// <exception cref="InvalidOperationException">The command already has an option with the
        ///     same long name.</exception>
        public CommandOption<string> AddOption(char? shortName, string? longName, string parameterName, string description, bool inherited = false)
        {
            return AddOption<string>(shortName, longName, parameterName, description, ParseString, inherited);
        }

        /// <summary>
        /// Adds a parameterized option with a specified value parser to the command.
        /// </summary>
        /// <param name="shortName">The short name of the option.</param>
        /// <param name="longName">The long name of the option.</param>
        /// <param name="parameterName">The name of the parameter of the option.</param>
        /// <param name="description">The description of the option.</param>
        /// <param name="parse">A delegate that attempts to parse a value for the parameter from
        ///     specified text.</param>
        /// <param name="inherited">Controls whether the command option is inherited by subcommands.
        ///     </param>
        /// <returns>The option that was added.</returns>
        /// <typeparam name="T">The type of value that the command option stores.</typeparam>
        /// <exception cref="ArgumentNullException"><paramref name="shortName"/> is
        ///     <see langword="null"/> and <paramref name="longName"/> is <see langword="null"/>.
        ///     </exception>
        /// <exception cref="ArgumentException"><paramref name="shortName"/> is invalid.</exception>
        /// <exception cref="ArgumentException"><paramref name="longName"/> is invalid.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="parameterName"/> is
        ///     <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="parameterName"/> is invalid.
        ///     </exception>
        /// <exception cref="ArgumentNullException"><paramref name="description"/> is
        ///     <see langword="null"/>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="parse"/> is
        ///     <see langword="null"/>.</exception>
        /// <exception cref="InvalidOperationException">The command already has an option with the
        ///     same short name.</exception>
        /// <exception cref="InvalidOperationException">The command already has an option with the
        ///     same long name.</exception>
        public CommandOption<T> AddOption<T>(char? shortName, string? longName, string parameterName, string description, TryParser<T> parse, bool inherited = false)
        {
            ValidateOptionNames(shortName, longName);
            ValidateOptionParameterName(parameterName);

            var option = new CommandOption<T>(shortName, longName, parameterName, description, parse, inherited);

            return AddOption(option);
        }

        /// <summary>
        /// Adds a parameterized option to the command.  The option can store multiple values.
        /// </summary>
        /// <param name="shortName">The short name of the option.</param>
        /// <param name="longName">The long name of the option.</param>
        /// <param name="parameterName">The name of the parameter of the option.</param>
        /// <param name="description">The description of the option.</param>
        /// <param name="inherited">Controls whether the command option is inherited by subcommands.
        ///     </param>
        /// <returns>The option that was added.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="shortName"/> is
        ///     <see langword="null"/> and <paramref name="longName"/> is <see langword="null"/>.
        ///     </exception>
        /// <exception cref="ArgumentException"><paramref name="shortName"/> is invalid.</exception>
        /// <exception cref="ArgumentException"><paramref name="longName"/> is invalid.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="parameterName"/> is
        ///     <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="parameterName"/> is invalid.
        ///     </exception>
        /// <exception cref="ArgumentNullException"><paramref name="description"/> is
        ///     <see langword="null"/>.</exception>
        /// <exception cref="InvalidOperationException">The command already has an option with the
        ///     same short name.</exception>
        /// <exception cref="InvalidOperationException">The command already has an option with the
        ///     same long name.</exception>
        public VariadicCommandOption<string> AddVariadicOption(char? shortName, string? longName, string parameterName, string description, bool inherited = false)
        {
            return AddVariadicOption<string>(shortName, longName, parameterName, description, ParseString, inherited);
        }

        /// <summary>
        /// Adds a parameterized option with a specified value parser to the command.  The option
        /// can store multiple values.
        /// </summary>
        /// <param name="shortName">The short name of the option.</param>
        /// <param name="longName">The long name of the option.</param>
        /// <param name="parameterName">The name of the parameter of the option.</param>
        /// <param name="description">The description of the option.</param>
        /// <param name="parse">A delegate that attempts to parse a value for the parameter from
        ///     specified text.</param>
        /// <param name="inherited">Controls whether the command option is inherited by subcommands.
        ///     </param>
        /// <returns>The option that was added.</returns>
        /// <typeparam name="T">The type of value that the command option stores.</typeparam>
        /// <exception cref="ArgumentNullException"><paramref name="shortName"/> is
        ///     <see langword="null"/> and <paramref name="longName"/> is <see langword="null"/>.
        ///     </exception>
        /// <exception cref="ArgumentException"><paramref name="shortName"/> is invalid.</exception>
        /// <exception cref="ArgumentException"><paramref name="longName"/> is invalid.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="parameterName"/> is
        ///     <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="parameterName"/> is invalid.
        ///     </exception>
        /// <exception cref="ArgumentNullException"><paramref name="description"/> is
        ///     <see langword="null"/>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="parse"/> is
        ///     <see langword="null"/>.</exception>
        /// <exception cref="InvalidOperationException">The command already has an option with the
        ///     same short name.</exception>
        /// <exception cref="InvalidOperationException">The command already has an option with the
        ///     same long name.</exception>
        public VariadicCommandOption<T> AddVariadicOption<T>(char? shortName, string? longName, string parameterName, string description, TryParser<T> parse, bool inherited = false)
        {
            ValidateOptionNames(shortName, longName);
            ValidateOptionParameterName(parameterName);

            var option = new VariadicCommandOption<T>(shortName, longName, parameterName, description, parse, inherited);

            return AddOption(option);
        }

        /// <summary>
        /// Sets the delegate that is invoked when the command is invoked.
        /// </summary>
        /// <param name="invoke">The delegate that is invoked when the command is invoked.</param>
        /// <exception cref="ArgumentNullException"><paramref name="invoke"/> is
        ///     <see langword="null"/>.</exception>
        public void SetInvokeHandler(Func<int> invoke)
        {
            if (invoke is null)
                throw new ArgumentNullException(nameof(invoke));

            _invoke = invoke;
        }

        /// <summary>
        /// Sets the delegate that is invoked when the command is invoked.
        /// </summary>
        /// <param name="invoke">The delegate that is invoked when the command is invoked.</param>
        /// <exception cref="ArgumentNullException"><paramref name="invoke"/> is
        ///     <see langword="null"/>.</exception>
        public void SetInvokeHandler(Func<Task<int>> invoke)
        {
            if (invoke is null)
                throw new ArgumentNullException(nameof(invoke));

            _invoke = () => invoke().GetAwaiter().GetResult();
        }

        /// <summary>
        /// Invokes the command.
        /// </summary>
        /// <returns>The return code from the invoke handler.</returns>
        public int Invoke() => _invoke();

        /// <summary>
        /// Sets the delegate that is invoked when the help option is specified.
        /// </summary>
        /// <param name="writeHelp">The delegate that is invoked when the help option is specified.
        ///     </param>
        /// <exception cref="ArgumentNullException"><paramref name="writeHelp"/> is
        ///     <see langword="null"/>.</exception>
        public void SetHelpHandler(HelpHandler writeHelp)
        {
            if (writeHelp is null)
                throw new ArgumentNullException(nameof(writeHelp));

            _writeHelp = writeHelp;
        }

        /// <summary>
        /// Writes help text for the command.  The default help handler writes to
        /// <see cref="Console.Error"/>.
        /// </summary>
        /// <param name="verbose">Controls whether the help text includes entities with
        ///     <see cref="HelpVisibility.Verbose"/> visibility.</param>
        /// <exception cref="IOException">An I/O error occurs when writing help text.</exception>
        public void WriteHelp(bool verbose) => _writeHelp(this, verbose);

        internal string GetCommandPath()
        {
            Command? command = this;

            string path = command.Name;

            for (; ; )
            {
                command = command.Parent;

                if (command is null)
                    return path;

                path = $"{command.Name} {path}";
            }
        }

        internal CommandOption? GetHelpOption()
        {
            if (HelpOption != null)
                return HelpOption;

            for (var command = Parent; command != null; command = command.Parent)
            {
                var helpOption = command.HelpOption;
                if (helpOption?.Inherited ?? false)
                    return helpOption;
            }

            return null;
        }

        internal CommandOption? GetVerboseOption()
        {
            if (VerboseOption != null)
                return VerboseOption;

            for (var command = Parent; command != null; command = command.Parent)
            {
                var verboseOption = command.VerboseOption;
                if (verboseOption?.Inherited ?? false)
                    return verboseOption;
            }

            return null;
        }

        internal List<CommandOption> GetOptions()
        {
            var result = new List<CommandOption>(_options);
            for (var command = Parent; command != null; command = command.Parent)
                foreach (var option in command._options)
                    if (option.Inherited)
                        result.Add(option);
            return result;
        }

        internal void Reset()
        {
            foreach (var subcommand in Subcommands)
                subcommand.Reset();

            foreach (var parameter in Parameters)
                parameter.Reset();

            foreach (var option in Options)
                option.Reset();
        }

        private static int DefaultInvoke() => -1;

        /// <exception cref="IOException">An I/O error occurs when writing help text.</exception>
        private void DefaultWriteHelp(Command command, bool verbose)
        {
            int columns;
            try
            {
                columns = Math.Max(80, Console.BufferWidth);
            }
            catch
            {
                columns = 80;
            }

            CommandHelp.WriteHelp(command, Console.Error, verbose, columns);
        }

        /// <exception cref="ArgumentNullException"><paramref name="text"/> is
        ///     <see langword="null"/>.</exception>
        private static bool ParseString(string text, out string value)
        {
            if (text is null)
                throw new ArgumentNullException(nameof(text));

            value = text;
            return true;
        }

        /// <exception cref="ArgumentNullException"><paramref name="name"/> is
        ///     <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="name"/> is invalid.</exception>
        private static void ValidateParameterName(string name)
        {
            if (name is null)
                throw new ArgumentNullException(nameof(name));
            if (name.Length == 0 || ContainsWhiteSpace(name))
                throw new ArgumentException("Invalid name.", nameof(name));
        }

        /// <exception cref="ArgumentException"><paramref name="shortName"/> is invalid.</exception>
        /// <exception cref="ArgumentException"><paramref name="longName"/> is invalid.</exception>
        private static void ValidateOptionNames(char? shortName, string? longName)
        {
            if (shortName == '-' || shortName == '=' || char.IsWhiteSpace(shortName.GetValueOrDefault()) || char.IsSurrogate(shortName.GetValueOrDefault()))
                throw new ArgumentException("Invalid name.", nameof(shortName));
            if (longName != null && (longName.Length == 0 || ContainsWhiteSpace(longName) || longName.IndexOf('=') >= 0))
                throw new ArgumentException("Invalid name.", nameof(longName));
        }

        /// <exception cref="ArgumentNullException"><paramref name="parameterName"/> is
        ///     <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="parameterName"/> is invalid.
        ///     </exception>
        private static void ValidateOptionParameterName(string parameterName)
        {
            if (parameterName is null)
                throw new ArgumentNullException(nameof(parameterName));
            if (parameterName.Length == 0 || ContainsWhiteSpace(parameterName))
                throw new ArgumentException("Invalid name.", nameof(parameterName));
        }

#pragma warning disable CA1704 // Identifiers should be spelled correctly
        private static bool ContainsWhiteSpace(string s)
#pragma warning restore CA1704 // Identifiers should be spelled correctly
        {
            foreach (char c in s)
                if (char.IsWhiteSpace(c))
                    return true;
            return false;
        }

        /// <exception cref="InvalidOperationException">The command has a variadic parameter.
        ///     </exception>
        /// <exception cref="InvalidOperationException">The command already has a parameter with the
        ///     same name.</exception>
        private T AddParameter<T>(T parameter)
            where T : CommandParameter
        {
            if (_parameters.Count > 0 && _parameters[_parameters.Count - 1].Variadic)
                throw new InvalidOperationException("The command has a variadic parameter, which must be the last parameter.");

            foreach (var existingParameter in _parameters)
                if (parameter.Name.Equals(existingParameter.Name, StringComparison.InvariantCulture))
                    throw new InvalidOperationException("The command already has a parameter with the same name.");

            _parameters.Add(parameter);

            return parameter;
        }

        /// <exception cref="InvalidOperationException">The command already has an option with the
        ///     same short name.</exception>
        /// <exception cref="InvalidOperationException">The command already has an option with the
        ///     same long name.</exception>
        private T AddOption<T>(T option)
            where T : CommandOption
        {
            foreach (var existingOption in _options)
            {
                if (option.ShortName != null && option.ShortName.Equals(existingOption.ShortName))
                    throw new InvalidOperationException("The command already has an option with the same short name.");
                if (option.LongName != null && option.LongName.Equals(existingOption.LongName, StringComparison.InvariantCulture))
                    throw new InvalidOperationException("The command already has an option with the same long name.");
            }

            CheckInherited(Parent, option);

            if (option.Inherited)
                CheckInheritors(_subcommands, option);

            _options.Add(option);

            return option;

            static void CheckInherited(Command? parentCommand, T option)
            {
                if (parentCommand is null)
                    return;

                foreach (var existingOption in parentCommand._options)
                {
                    if (!existingOption.Inherited)
                        continue;

                    if (option.ShortName != null && option.ShortName.Equals(existingOption.ShortName))
                        throw new InvalidOperationException("The command already inherits an option with the same short name.");
                    if (option.LongName != null && option.LongName.Equals(existingOption.LongName, StringComparison.InvariantCulture))
                        throw new InvalidOperationException("The command already inherits an option with the same long name.");
                }

                CheckInherited(parentCommand.Parent, option);
            }

            static void CheckInheritors(List<Command> subcommands, T option)
            {
                foreach (var subcommand in subcommands)
                {
                    foreach (var existingOption in subcommand._options)
                    {
                        if (option.ShortName != null && option.ShortName.Equals(existingOption.ShortName))
                            throw new InvalidOperationException("A subcommand already has an option with the same short name.");
                        if (option.LongName != null && option.LongName.Equals(existingOption.LongName, StringComparison.InvariantCulture))
                            throw new InvalidOperationException("A subcommand already has an option with the same long name.");
                    }

                    CheckInheritors(subcommand._subcommands, option);
                }
            }
        }

        /// <summary>
        /// Writes help text for a command.
        /// </summary>
        /// <param name="command">The command.</param>
        /// <param name="verbose">Controls whether the help text includes entities with
        ///     <see cref="HelpVisibility.Verbose"/> visibility.</param>
        /// <exception cref="ArgumentNullException"><paramref name="command"/> is
        ///     <see langword="null"/>.</exception>
        /// <exception cref="IOException">An I/O error occurs when writing help text.</exception>
        public delegate void HelpHandler(Command command, bool verbose);
    }
}
