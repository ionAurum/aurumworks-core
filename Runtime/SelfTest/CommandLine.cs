using System;

namespace Aurumworks
{
    /// <summary>
    /// Static: the flags the player was started with. A game gives its own flags its own prefix
    /// (-squeezeSeed 7, -blobMenuCheck), so none can be taken for Unity's or another game's.
    /// </summary>
    public static class CommandLine
    {
        private static string[] _arguments;

        private static string[] Arguments => _arguments ??= Environment.GetCommandLineArgs();

        /// <summary>True when the flag was given.</summary>
        public static bool Has(string flag) => Array.IndexOf(Arguments, flag) >= 0;

        /// <summary>
        /// The argument that follows the flag (the first, or the one at position for a flag that
        /// takes several), or null when the flag wasn't given or has no such argument.
        /// </summary>
        public static string Value(string flag, int position = 1)
        {
            int index = Array.IndexOf(Arguments, flag);
            return index >= 0 && index + position < Arguments.Length ? Arguments[index + position] : null;
        }
    }
}
