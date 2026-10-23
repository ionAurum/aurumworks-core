using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Audio;

namespace Aurumworks
{
    /// <summary>
    /// A cue built in code, for a game without cue assets: its variations are every clip in a folder
    /// under a Resources folder (subfolders included), loaded the first time it plays. Adding a
    /// variation is dropping a file in, and a cue with no files yet is simply silent.
    /// </summary>
    public sealed class SoundFolderCue : ISoundCue
    {
        private readonly Dictionary<string, SoundFolderCue> _variants = new();
        private SoundTake[] _takes;

        /// <param name="folder">The clips' folder, as Resources.LoadAll takes it ("Sounds/Film/Gunshot").</param>
        /// <param name="bus">The bus it follows, or null for none.</param>
        /// <param name="name">What the play counts call it; by default the folder's own name.</param>
        public SoundFolderCue(string folder, SoundBus bus = null, string name = null)
        {
            Folder = folder;
            Bus = bus;
            Name = name ?? folder.Substring(folder.LastIndexOf('/') + 1);
        }

        /// <summary>
        /// Self-test aid: a cue with no clips plays a short tone of its own pitch (a two-second hum
        /// for a loop), so every sound's path can be exercised. Set it before any cue loads.
        /// </summary>
        public static bool TestTones { get; set; }

        // Statics survive play mode when domain reload is disabled; reset them.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => TestTones = false;

        /// <inheritdoc/>
        public string Name { get; }

        /// <summary>The clips' folder, under a Resources folder.</summary>
        public string Folder { get; }

        /// <inheritdoc/>
        public SoundBus Bus { get; }

        /// <summary>Volume, before the spread and the caller's scale.</summary>
        public float Volume { get; set; } = 1f;

        /// <summary>How far below Volume a play may fall at random, as a share of it.</summary>
        public float VolumeSpread { get; set; } = 0.1f;

        /// <summary>How far either side of its own pitch a play may fall at random.</summary>
        public float PitchSpread { get; set; } = 0.05f;

        /// <summary>0 plays in 2D; up to 1 plays fully in 3D at a position.</summary>
        public float Spatial { get; set; }

        /// <inheritdoc/>
        public float MinDistance { get; set; } = 6f;

        /// <inheritdoc/>
        public float MaxDistance { get; set; } = 60f;

        /// <inheritdoc/>
        public int MaxVoices { get; set; } = 4;

        /// <inheritdoc/>
        public float Cooldown { get; set; } = 0.03f;

        /// <inheritdoc/>
        public int Priority { get; set; } = 128;

        /// <summary>Whether it is played as a loop, from anywhere in its clip.</summary>
        public bool Loop { get; set; }

        /// <inheritdoc/>
        public float FadeTime { get; set; } = 0.25f;

        /// <inheritdoc/>
        public bool DucksMusic { get; set; }

        /// <inheritdoc/>
        public float MaxLength { get; set; }

        /// <summary>
        /// Whether its clips are measured as they load (SoundLevelling): a one-shot starts where its
        /// sound does, and the variations are evened in loudness. For clips decompressed on load;
        /// leave it off for long clips kept compressed in memory, which can't be read.
        /// </summary>
        public bool Measured { get; set; }

        /// <summary>
        /// The loudness (dBFS) every variation of a measured cue is brought to; null evens them to
        /// the cue's middle.
        /// </summary>
        public float? Level { get; set; }

        /// <summary>The variations, loaded (and measured) the first time they're asked for.</summary>
        public SoundTake[] Takes
        {
            get
            {
                if (_takes == null)
                {
                    var clips = Resources.LoadAll<AudioClip>(Folder);
                    if (clips.Length == 0 && TestTones)
                    {
                        clips = new[] { Tone(Name, Loop) };
                    }

                    _takes = Measured
                        ? SoundLevelling.Measure(clips, !Loop, Level)
                        : clips.Select(clip => new SoundTake(clip)).ToArray();
                }

                return _takes;
            }
        }

        /// <summary>Whether it has anything to play.</summary>
        public bool HasClips => Takes.Length > 0;

        /// <summary>
        /// The cue with only the clips of one of its subfolders (a voice, a surface), played like the
        /// cue and counted as "Name (subfolder)". Without that subfolder, the cue itself.
        /// </summary>
        public SoundFolderCue Variant(string subfolder)
        {
            if (!_variants.TryGetValue(subfolder, out var cue))
            {
                cue = new SoundFolderCue(Folder + "/" + subfolder, Bus, Name + " (" + subfolder + ")")
                {
                    Volume = Volume,
                    VolumeSpread = VolumeSpread,
                    PitchSpread = PitchSpread,
                    Spatial = Spatial,
                    MinDistance = MinDistance,
                    MaxDistance = MaxDistance,
                    MaxVoices = MaxVoices,
                    Cooldown = Cooldown,
                    Priority = Priority,
                    Loop = Loop,
                    FadeTime = FadeTime,
                    DucksMusic = DucksMusic,
                    MaxLength = MaxLength,
                    Measured = Measured,
                    Level = Level,
                };
                if (!cue.HasClips)
                {
                    cue = this;
                }

                _variants[subfolder] = cue;
            }

            return cue;
        }

        int ISoundCue.TakeCount => Takes.Length;
        SoundTake ISoundCue.GetTake(int index) => Takes[index];
        AudioMixerGroup ISoundCue.Output => null;
        Vector2 ISoundCue.Volume => new Vector2(Volume * (1f - VolumeSpread), Volume);
        Vector2 ISoundCue.Pitch => new Vector2(1f - PitchSpread, 1f + PitchSpread);
        float ISoundCue.SpatialBlend => Spatial;

        /// <summary>A short tone pitched by the cue's name (a two-second hum for a loop).</summary>
        private static AudioClip Tone(string name, bool loop)
        {
            const int rate = 22050;
            float seconds = loop ? 2f : 0.18f;
            int count = Mathf.RoundToInt(rate * seconds);
            float frequency = 220f + (uint)name.GetHashCode() % 660u;
            var samples = new float[count];
            for (int i = 0; i < count; i++)
            {
                float time = i / (float)rate;
                float envelope = loop ? 0.25f : Mathf.Clamp01(1f - time / seconds) * 0.4f;
                samples[i] = Mathf.Sin(2f * Mathf.PI * frequency * time) * envelope;
            }

            var clip = AudioClip.Create("Tone " + name, count, 1, rate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}
