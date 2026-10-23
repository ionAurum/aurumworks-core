using System;
using System.Collections.Generic;
using UnityEngine;

namespace Aurumworks
{
    /// <summary>
    /// Static: this machine's settings. What every game has is here: fullscreen or a window, the
    /// volumes the game lists (AddVolume), and saving. A game's own settings (a name, mouse
    /// sensitivity) are values it keeps here by key. Values are in PlayerPrefs, under keys named
    /// Aurumworks.Group.Name; the window mode is Unity's to remember. A value a game saved under
    /// another key before it took these settings is read from that old key, and saved under the new
    /// one at the next Save. A self-test reads the player's settings and writes none: what it sets
    /// lasts for the run.
    /// </summary>
    public static class Settings
    {
        private const float WindowedShare = 2f / 3f; // a new window's size, as a share of its display's

        private static readonly List<VolumeSetting> _volumes = new();
        private static readonly Dictionary<string, float> _floats = new();
        private static readonly Dictionary<string, string> _strings = new();

        // The keys whose values came from an old key, still to be written under their own.
        private static readonly HashSet<string> _fromOldKeys = new();

        // The window's size when it last went fullscreen, to go back to.
        private static Vector2Int? _windowedSize;

        /// <summary>The volumes the game has listed, in the order the Settings page shows them.</summary>
        public static IReadOnlyList<VolumeSetting> Volumes => _volumes;

        /// <summary>Raised when a volume is set: whatever plays sound takes the new value.</summary>
        public static event Action<VolumeSetting> VolumeChanged;

        /// <summary>
        /// True in fullscreen, false in a window. Unity remembers the window mode and size between runs
        /// (on Windows, in the registry), so it isn't kept in PlayerPrefs. Alt+Enter toggles it too.
        /// </summary>
        public static bool IsFullScreen => Screen.fullScreenMode != FullScreenMode.Windowed;

        // Statics survive play mode when domain reload is disabled; reset them. Each game lists its
        // volumes again as it starts.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            _volumes.Clear();
            _floats.Clear();
            _strings.Clear();
            _fromOldKeys.Clear();
            _windowedSize = null;
            VolumeChanged = null;
        }

        //----------

        /// <summary>
        /// Switches between borderless fullscreen at the display's resolution and a window: the size
        /// it had before going fullscreen, else two thirds of the display. Takes effect at the end of
        /// the frame. Does nothing in the editor.
        /// </summary>
        public static void SetFullScreen(bool fullScreen)
        {
            if (fullScreen == IsFullScreen)
            {
                return;
            }

            // The display the window is on, which isn't always the main one.
            var display = Screen.mainWindowDisplayInfo;
            if (fullScreen)
            {
                _windowedSize = new Vector2Int(Screen.width, Screen.height);
                Screen.SetResolution(display.width, display.height, FullScreenMode.FullScreenWindow);
            }
            else
            {
                var size = _windowedSize ?? new Vector2Int(
                    Mathf.RoundToInt(display.width * WindowedShare), Mathf.RoundToInt(display.height * WindowedShare));
                Screen.SetResolution(size.x, size.y, FullScreenMode.Windowed);
            }
        }

        /// <summary>
        /// Lists a volume, as the game starts: the Settings page shows a slider for each, in the
        /// order listed. The name is the slider's label and the key's (Aurumworks.Volume.Name).
        /// Listing a name again returns the volume already listed.
        /// </summary>
        public static VolumeSetting AddVolume(string name, float defaultValue = 1f, string oldKey = null)
        {
            foreach (var listed in _volumes)
            {
                if (listed.Name == name)
                {
                    return listed;
                }
            }

            var volume = new VolumeSetting(name, defaultValue, oldKey);
            _volumes.Add(volume);
            return volume;
        }

        /// <summary>A number the game keeps, or defaultValue if it was never set.</summary>
        public static float GetFloat(string key, float defaultValue, string oldKey = null)
        {
            if (!_floats.TryGetValue(key, out float value))
            {
                string stored = StoredKey(key, oldKey);
                value = stored != null ? PlayerPrefs.GetFloat(stored) : defaultValue;
                _floats[key] = value;
            }

            return value;
        }

        public static void SetFloat(string key, float value)
        {
            _floats[key] = value;
            _fromOldKeys.Remove(key);
            if (!SelfTest.IsRunning)
            {
                PlayerPrefs.SetFloat(key, value);
            }
        }

        /// <summary>A text the game keeps, or defaultValue if it was never set.</summary>
        public static string GetString(string key, string defaultValue, string oldKey = null)
        {
            if (!_strings.TryGetValue(key, out string value))
            {
                string stored = StoredKey(key, oldKey);
                value = stored != null ? PlayerPrefs.GetString(stored) : defaultValue;
                _strings[key] = value;
            }

            return value;
        }

        public static void SetString(string key, string value)
        {
            _strings[key] = value;
            _fromOldKeys.Remove(key);
            if (!SelfTest.IsRunning)
            {
                PlayerPrefs.SetString(key, value);
            }
        }

        /// <summary>
        /// Writes the settings to disk now; PlayerPrefs otherwise only saves on a clean quit. The
        /// Settings page calls it as it closes.
        /// </summary>
        public static void Save()
        {
            if (SelfTest.IsRunning)
            {
                return;
            }

            foreach (string key in _fromOldKeys)
            {
                if (_floats.TryGetValue(key, out float number))
                {
                    PlayerPrefs.SetFloat(key, number);
                }
                else
                {
                    PlayerPrefs.SetString(key, _strings[key]);
                }
            }

            _fromOldKeys.Clear();
            PlayerPrefs.Save();
        }

        //----------

        internal static void RaiseVolumeChanged(VolumeSetting volume) => VolumeChanged?.Invoke(volume);

        // Where a value is: under its own key, under the game's old key for it, or nowhere (null).
        // The old key stays as it is, so an older build of the game still finds its settings.
        private static string StoredKey(string key, string oldKey)
        {
            if (PlayerPrefs.HasKey(key))
            {
                return key;
            }

            if (oldKey != null && PlayerPrefs.HasKey(oldKey))
            {
                _fromOldKeys.Add(key);
                return oldKey;
            }

            return null;
        }
    }
}
