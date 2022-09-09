// Copyright 2022 Carl Reinke
//
// This file is part of a library that is licensed under the terms of the GNU
// Lesser General Public License Version 3 as published by the Free Software
// Foundation.
//
// This license does not grant rights under trademark law for use of any trade
// names, trademarks, or service marks.

namespace Tetractic.CommandLine
{
    /// <summary>
    /// Specifies when entities appears in the help text for a command.
    /// </summary>
    public enum HelpVisibility
    {
        /// <summary>
        /// The entity always appears in the help text.
        /// </summary>
        Always = 0,

        /// <summary>
        /// The entity only appears in the verbose help text.
        /// </summary>
        Verbose = 1,

        /// <summary>
        /// The entity never appears in the help text.
        /// </summary>
        Never = 2,
    }
}
