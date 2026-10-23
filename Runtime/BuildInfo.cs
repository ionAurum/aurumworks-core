using UnityEngine;

namespace Aurumworks
{
    /// <summary>
    /// The running build's version, shown on the main menu and written to the log at startup, so a bug
    /// report says which build it came from. Bump Project Settings > Player > Version before sharing a
    /// build.
    /// </summary>
    public static class BuildInfo
    {
        /// <summary>For example "v0.1.0", "v0.1.0 (development build)" or "v0.1.0 (editor)".</summary>
        public static string Version
        {
            get
            {
                string kind = Application.isEditor ? " (editor)"
                            : Debug.isDebugBuild ? " (development build)"
                            : "";
                return $"v{Application.version}{kind}";
            }
        }
    }
}
