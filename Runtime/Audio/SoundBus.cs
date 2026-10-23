using UnityEngine.Audio;

namespace Aurumworks
{
    /// <summary>
    /// A group of cues the game turns up, down, slows or muffles together ("Effects", "Music"). The
    /// game makes its buses in code, names one in each cue, and sets their values as often as it
    /// likes; every voice playing on a bus follows it from the next frame. A game with an AudioMixer
    /// can give a bus a mixer group and set volumes there instead. Cues without a bus play as they are.
    /// </summary>
    public sealed class SoundBus
    {
        /// <summary>A low-pass cutoff that lets everything through.</summary>
        public const float Open = 22000f;

        /// <param name="name">What the game calls it.</param>
        /// <param name="lowPass">Whether its voices get a low-pass filter, so Cutoff can muffle them.</param>
        public SoundBus(string name, bool lowPass = false)
        {
            Name = name;
            LowPass = lowPass;
        }

        /// <summary>What the game calls it.</summary>
        public string Name { get; }

        /// <summary>Whether its voices have a low-pass filter.</summary>
        public bool LowPass { get; }

        /// <summary>Mixer group for its cues, or null for the SoundPlayer's default.</summary>
        public AudioMixerGroup Output { get; set; }

        /// <summary>Volume multiplier, as an amplitude.</summary>
        public float Volume { get; set; } = 1f;

        /// <summary>Pitch multiplier.</summary>
        public float Pitch { get; set; } = 1f;

        /// <summary>Low-pass cutoff in hertz; Open leaves the sound as it is. Needs LowPass.</summary>
        public float Cutoff { get; set; } = Open;

        /// <summary>Whether its sounds skip the scene's reverb zones (controls, music).</summary>
        public bool BypassReverbZones { get; set; }
    }
}
