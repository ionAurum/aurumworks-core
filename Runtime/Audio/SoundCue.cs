using UnityEngine;
using UnityEngine.Audio;

namespace Aurumworks
{
    /// <summary>
    /// One sound event ("Pop", "UI Hover"): its variations and how they play. Code refers to cues,
    /// never to clips, so adding a variation is an Inspector change. SoundPlayer deals the clips from
    /// a shuffled deck: never the same one twice in a row, and every clip before any repeats.
    /// The cue as an asset; SoundFolderCue is the kind built in code.
    /// </summary>
    [CreateAssetMenu(menuName = "Aurumworks/Sound Cue", fileName = "Sound Cue")]
    public class SoundCue : ScriptableObject, ISoundCue
    {
        [Tooltip("Variations, one picked per play.")]
        [SerializeField] private AudioClip[] _clips = new AudioClip[0];
        [Tooltip("Mixer group. Empty uses the SoundPlayer's Default Output.")]
        [SerializeField] private AudioMixerGroup _output;

        [Header("Variety")]
        [Tooltip("Volume, from x to y at random, before the caller's scale.")]
        [SerializeField] private Vector2 _volume = new(0.9f, 1f);
        [Tooltip("Pitch, from x to y at random, before the caller's scale.")]
        [SerializeField] private Vector2 _pitch = new(0.95f, 1.05f);

        [Header("Space")]
        [Tooltip("Off for UI sounds and chimes, which everyone hears the same.")]
        [SerializeField] private bool _spatial = true;
        [Tooltip("Full volume within this distance. Callers may scale it per play (a bigger thing can carry further).")]
        [SerializeField] private float _minDistance = 3f;
        [SerializeField] private float _maxDistance = 80f;

        [Header("Limits")]
        [Tooltip("Plays at once, at most. Another play restarts the oldest.")]
        [Min(1)]
        [SerializeField] private int _maxVoices = 4;
        [Tooltip("Seconds after a play in which another is skipped. Stops hover chatter and pile-ups.")]
        [SerializeField] private float _cooldown = 0.05f;
        [Tooltip("0 is most important, 256 least: which sounds Unity drops first when out of real voices.")]
        [Range(0, 256)]
        [SerializeField] private int _priority = 128;

        [Header("Loops")]
        [Tooltip("Seconds to fade a loop in or out, and to follow volume changes.")]
        [SerializeField] private float _fadeTime = 0.25f;

        /// <summary>The variations.</summary>
        public AudioClip[] Clips => _clips;

        /// <summary>Mixer group, or null for the SoundPlayer's default.</summary>
        public AudioMixerGroup Output => _output;

        /// <summary>Volume range, picked from at random per play.</summary>
        public Vector2 Volume => _volume;

        /// <summary>Pitch range, picked from at random per play.</summary>
        public Vector2 Pitch => _pitch;

        /// <summary>Whether it plays in 3D at a position.</summary>
        public bool Spatial => _spatial;

        /// <summary>Full volume within this distance (before the caller's scale).</summary>
        public float MinDistance => _minDistance;

        /// <summary>Silent past this distance (before the caller's scale).</summary>
        public float MaxDistance => _maxDistance;

        /// <summary>Plays at once, at most.</summary>
        public int MaxVoices => _maxVoices;

        /// <summary>Seconds after a play in which another is skipped.</summary>
        public float Cooldown => _cooldown;

        /// <summary>AudioSource priority: 0 most important, 256 least.</summary>
        public int Priority => _priority;

        /// <summary>Seconds a loop takes to fade in or out.</summary>
        public float FadeTime => _fadeTime;

        /// <summary>Whether it has anything to play.</summary>
        public bool HasClips => _clips != null && _clips.Length > 0;

        // What an asset doesn't have yet: its clips play whole and as they are, through a mixer group.
        string ISoundCue.Name => name;
        int ISoundCue.TakeCount => _clips != null ? _clips.Length : 0;
        SoundTake ISoundCue.GetTake(int index) => new SoundTake(_clips[index]);
        SoundBus ISoundCue.Bus => null;
        float ISoundCue.SpatialBlend => _spatial ? 1f : 0f;
        float ISoundCue.MaxLength => 0f;
        bool ISoundCue.DucksMusic => false;
    }
}
