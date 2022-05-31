// Copyright 2022 Carl Reinke
//
// This file is part of a library that is licensed under the terms of GNU Lesser
// General Public License version 3 as published by the Free Software
// Foundation.
//
// This license does not grant rights under trademark law for use of any trade
// names, trademarks, or service marks.

using System;

namespace Tetractic.CommandLine
{
    /// <summary>
    /// Represents an error caused by invalid command-line arguments.
    /// </summary>
#pragma warning disable CA1032 // Implement standard exception constructors -- Justification: Exception is not intended for general-purpose use.
    public sealed class InvalidCommandLineException : Exception
#pragma warning restore CA1032 // Implement standard exception constructors
    {
        /// <summary>
        /// Initializes a new <see cref="InvalidCommandLineException"/>.
        /// </summary>
        /// <param name="command">The command that was active when the command-line argument were
        ///     determined to be invalid.</param>
        /// <param name="message">A description of the error.</param>
        /// <exception cref="ArgumentNullException"><paramref name="command"/> is
        ///     <see langword="null"/>.</exception>
        public InvalidCommandLineException(Command command, string? message)
            : base(message)
        {
            if (command is null)
                throw new ArgumentNullException(nameof(command));

            Command = command;
        }

        /// <summary>
        /// Gets the command that was active when the command-line arguments were determined to be
        /// invalid.
        /// </summary>
        public Command Command { get; }
    }
}
