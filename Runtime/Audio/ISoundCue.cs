using UnityEngine;
using UnityEngine.Audio;

namespace Aurumworks
{
    /// <summary>
    /// One sound event as SoundPlayer plays it: its variations and how they play. Two kinds: SoundCue,
    /// an asset tuned in the Inspector, and SoundFolderCue, built in code from a Resources folder.
    /// Code refers to cues, never to clips.
    /// </summary>
    public interface ISoundCue
    {
        /// <summary>What the play counts call it.</summary>
        string Name { get; }

        /// <summary>How many variations it has; none plays nothing.</summary>
        int TakeCount { get; }

        /// <summary>A variation, from 0 to TakeCount - 1.</summary>
        SoundTake GetTake(int index);

        /// <summary>The bus whose volume, pitch and muffling it follows, or null for none.</summary>
        SoundBus Bus { get; }

        /// <summary>Mixer group, or null for the bus's, else the SoundPlayer's default.</summary>
        AudioMixerGroup Output { get; }

        /// <summary>Volume range, picked from at random per play.</summary>
        Vector2 Volume { get; }

        /// <summary>Pitch range, picked from at random per play.</summary>
        Vector2 Pitch { get; }

        /// <summary>0 plays in 2D; up to 1 plays fully in 3D at a position.</summary>
        float SpatialBlend { get; }

        /// <summary>Full volume within this distance (before the caller's scale).</summary>
        float MinDistance { get; }

        /// <summary>Silent past this distance (before the caller's scale).</summary>
        float MaxDistance { get; }

        /// <summary>Plays at once, at most. Another play restarts the oldest.</summary>
        int MaxVoices { get; }

        /// <summary>Seconds after a play in which another is skipped.</summary>
        float Cooldown { get; }

        /// <summary>AudioSource priority: 0 most important, 256 least.</summary>
        int Priority { get; }

        /// <summary>Seconds a loop takes to fade in or out.</summary>
        float FadeTime { get; }

        /// <summary>
        /// A one-shot plays at most this many seconds, fading out over the last quarter second;
        /// 0 plays it all.
        /// </summary>
        float MaxLength { get; }

        /// <summary>Whether music should dip under it (SoundPlayer.SecondsSinceDuck).</summary>
        bool DucksMusic { get; }
    }

    /// <summary>
    /// A clip as a cue plays it: from where its sound starts, at a gain that evens it with the cue's
    /// other variations (SoundLevelling). A clip played as it is starts at 0 with a gain of 1.
    /// </summary>
    public readonly struct SoundTake
    {
        public SoundTake(AudioClip clip, int start = 0, float gain = 1f, float boost = 1f)
        {
            Clip = clip;
            Start = start;
            Gain = gain;
            Boost = boost;
        }

        /// <summary>What plays.</summary>
        public AudioClip Clip { get; }

        /// <summary>The sample a one-shot plays from.</summary>
        public int Start { get; }

        /// <summary>The volume the clip is played at, on top of the cue's.</summary>
        public float Gain { get; }

        /// <summary>How much louder than its file the clip was made (1: not at all).</summary>
        public float Boost { get; }
    }
}
