using UnityEngine;

namespace Aurumworks
{
    /// <summary>
    /// A looping sound started by SoundPlayer.StartLoop. The owner sets Volume, Pitch and Position as
    /// often as it likes; SoundPlayer fades toward them, and fades out and frees the voice after Stop.
    /// Once stopped, the handle does nothing: start a new loop instead.
    /// </summary>
    public sealed class SoundLoop
    {
        private readonly AudioSource _source;
        private float _volume = 1f;
        private float _pitch = 1f;

        internal SoundLoop(AudioSource source)
        {
            _source = source;
        }

        /// <summary>Volume multiplier, faded toward over the cue's Fade Time.</summary>
        public float Volume
        {
            get => _volume;
            set => _volume = Mathf.Max(value, 0f);
        }

        /// <summary>Pitch multiplier, applied at once.</summary>
        public float Pitch
        {
            get => _pitch;
            set => _pitch = Mathf.Max(value, 0.01f);
        }

        /// <summary>Where a spatial loop plays from.</summary>
        public Vector3 Position
        {
            set
            {
                if (!IsReleased)
                {
                    _source.transform.position = value;
                }
            }
        }

        /// <summary>False once stopped (fading out or freed).</summary>
        public bool IsPlaying => !IsStopping && !IsReleased;

        internal float Gain { get; set; }
        internal bool IsStopping { get; private set; }
        internal bool IsReleased { get; set; }

        /// <summary>Fades out over the cue's Fade Time, then frees the voice.</summary>
        public void Stop() => IsStopping = true;
    }
}
