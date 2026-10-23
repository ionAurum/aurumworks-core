using UnityEngine;

namespace Aurumworks
{
    /// <summary>
    /// One of a game's volumes (Master, Music), as its slider on the Settings page shows it and as
    /// whatever plays sound needs it. A game lists its volumes with Settings.AddVolume.
    /// </summary>
    public sealed class VolumeSetting
    {
        private const float SilentDecibels = -80f; // an AudioMixer's floor
        private const float SilentVolume = 0.01f;  // where the volume curve reaches it

        private readonly string _key;
        private readonly string _oldKey;
        private readonly float _defaultValue;

        internal VolumeSetting(string name, float defaultValue, string oldKey)
        {
            Name = name;
            _key = "Aurumworks.Volume." + name;
            _oldKey = oldKey;
            _defaultValue = defaultValue;
        }

        /// <summary>The slider's label.</summary>
        public string Name { get; }

        /// <summary>From 0 (silent) to 1 (full), as the slider shows it.</summary>
        public float Value
        {
            get => Mathf.Clamp01(Settings.GetFloat(_key, _defaultValue, _oldKey));
            set
            {
                Settings.SetFloat(_key, Mathf.Clamp01(value));
                Settings.RaiseVolumeChanged(this);
            }
        }

        /// <summary>
        /// The volume as an amplitude, for a game that sets volumes in code. The value is squared, so
        /// halfway is −12 dB, which sounds about half as loud (−6 dB would still sound three-quarters
        /// as loud), and the bottom of a slider isn't crowded with all the quiet settings.
        /// </summary>
        public float Amplitude => Value * Value;

        /// <summary>The same curve in an AudioMixer's decibels. 0.01 and below are silent.</summary>
        public float Decibels => Value > SilentVolume ? 40f * Mathf.Log10(Value) : SilentDecibels;
    }
}
