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
    /// Represents a command option that stores a value or multiple values.
    /// </summary>
    public abstract class ParameterizedCommandOption<T> : ParameterizedCommandOption
    {
        private protected T _optionalParameterDefaultValue = default!;

        internal ParameterizedCommandOption(char? shortName, string? longName, string parameterName, string description, bool inherited)
            : base(shortName, longName, parameterName, description, inherited)
        {
        }

        /// <summary>
        /// Gets the default value of the command option parameter if it is optional.
        /// </summary>
        /// <exception cref="InvalidOperationException">The command option parameter is not
        ///     optional.</exception>
        public T OptionalParameterDefaultValue
        {
            get
            {
                if (!ParameterIsOptional)
                    throw new InvalidOperationException("The command option parameter is not optional.");

                return _optionalParameterDefaultValue;
            }
        }

        /// <summary>
        /// Sets the default value of the command option parameter.  This makes the command option
        /// parameter optional.
        /// </summary>
        /// <param name="defaultValue">The value to be used when the command option parameter value
        ///     is omitted.</param>
        public void SetOptionalParameterDefaultValue(T defaultValue)
        {
            _optionalParameterDefaultValue = defaultValue;

            ParameterIsOptional = true;
        }
    }
}
