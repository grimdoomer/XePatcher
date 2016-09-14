using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace XePatcher
{
    /// <summary>
    /// Provides utilities to parse and scan a command line array.
    /// </summary>
    public class CommandLine
    {
        private string[] commandLine;
        /// <summary>
        /// Gets the command line arguments.
        /// </summary>
        public string[] CommandLineArgs { get { return commandLine; } }

        /// <summary>
        /// Creates a new CommandLine object using the specified command line argument array.
        /// </summary>
        /// <param name="args">Command line argument array.</param>
        public CommandLine(string[] args)
        {
            // Save command line args.
            this.commandLine = args;
        }

        /// <summary>
        /// Searches the command line arguments for an option variable.
        /// </summary>
        /// <param name="option">Option variable to search for.</param>
        /// <param name="isCaseSensitive">Boolean indicating the option name is case sensitive.</param>
        /// <returns>True if the command line contains the specified option, false otherwise.</returns>
        public bool FindOption(string option, bool isCaseSensitive)
        {
            // Loop through the command line args.
            for (int i = 0; i < commandLine.Length; i++)
            {
                // Check if the strings match.
                if ((isCaseSensitive == true && option.Equals(commandLine[i], StringComparison.Ordinal)) ||
                    (isCaseSensitive == false && option.Equals(commandLine[i], StringComparison.OrdinalIgnoreCase)))
                    return true;
            }

            // Option was not found in the command line array.
            return false;
        }

        /// <summary>
        /// Searches the command line arguments for the specified key value.
        /// </summary>
        /// <param name="key">Key value to search for.</param>
        /// <param name="isCaseSensitive">Boolean indicating the key name is case sensitive.</param>
        /// <returns>True if the command line contains the specified key, false otherwise.</returns>
        public bool IsKeyPresent(string key, bool isCaseSensitive)
        {
            // Loop through the command line args.
            for (int i = 0; i < commandLine.Length; i++)
            {
                // Check if the strings match.
                if ((isCaseSensitive == true && key.Equals(commandLine[i], StringComparison.Ordinal)) ||
                    (isCaseSensitive == false && key.Equals(commandLine[i], StringComparison.OrdinalIgnoreCase)))
                    return true;
            }

            // Key was not found in the command line array.
            return false;
        }

        /// <summary>
        /// Gets the value for the specified key in the command line arguments.
        /// </summary>
        /// <param name="key">Key value to search for.</param>
        /// <param name="isCaseSensitive">Boolean indicating the key name is case sensitive.</param>
        /// <returns>The value of the key if the key and value exist, else the return value is null.</returns>
        public string GetKeyValue(string key, bool isCaseSensitive)
        {
            // Loop through the command line args.
            for (int i = 0; i < commandLine.Length; i++)
            {
                // Check if the strings match.
                if ((isCaseSensitive == true && key.Equals(commandLine[i], StringComparison.Ordinal)) ||
                    (isCaseSensitive == false && key.Equals(commandLine[i], StringComparison.OrdinalIgnoreCase)))
                {
                    // Check if there is another command line argument.
                    if (i == commandLine.Length - 1)
                        return null;

                    // Return the key value.
                    return commandLine[i + 1];
                }
            }

            // Key was not found in the command line array.
            return null;
        }
    }
}
