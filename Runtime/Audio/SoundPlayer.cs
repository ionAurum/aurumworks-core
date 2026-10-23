using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Audio;

namespace Aurumworks
{
    /// <summary>
    /// Plays sound cues (ISoundCue: SoundCue assets, or SoundFolderCues built in code) from a pool of
    /// AudioSources: one-shots at a position or in 2D, and loops whose owners drive their volume,
    /// pitch and position. Deals each cue's clips from a shuffled deck, enforces its cooldown and
    /// voice limit, and when the pool is full takes over the oldest one-shot (loops are never taken).
    /// A one-shot starts where its take says, at the take's gain, and stops at its cue's Max Length,
    /// or when Stop names its cue.
    /// A voice whose cue names a bus follows that bus's volume, pitch and muffling as it plays.
    /// Put one in a scene that stays loaded for the whole session, or make one with Create.
    /// Local only: online, every machine plays its own sounds from what it sees.
    /// </summary>
    public class SoundPlayer : MonoBehaviour
    {
        // A one-shot cut short, at its cue's Max Length or by Stop, fades out over this long.
        private const float CutFade = 0.25f;

        private static readonly Dictionary<string, int> _playCounts = new();

        [Tooltip("AudioSources in the pool: the most sounds at once.")]
        [Min(1)]
        [SerializeField] private int _voiceCount = 24;
        [Tooltip("Mixer group for cues that don't name one.")]
        [SerializeField] private AudioMixerGroup _defaultOutput;

        private Voice[] _voices;
        private readonly Dictionary<ISoundCue, CueState> _cueStates = new();
        private float _duckTime = float.NegativeInfinity;

        /// <summary>The session's sound player, or null before its scene has loaded.</summary>
        public static SoundPlayer Instance { get; private set; }

        /// <summary>
        /// Seconds since a cue that ducks music last played (infinity if none has), for whatever
        /// plays the music to dip under it.
        /// </summary>
        public static float SecondsSinceDuck => Instance != null ? Time.unscaledTime - Instance._duckTime : float.PositiveInfinity;

        // Statics survive play mode when domain reload is disabled; reset them.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Instance = null;
            _playCounts.Clear();
        }

        /// <summary>Makes the sound player in code, under parent, for a game built without scenes.</summary>
        public static SoundPlayer Create(Transform parent)
        {
            var player = new GameObject("Sound Player").AddComponent<SoundPlayer>();
            player.transform.SetParent(parent, false);
            return player;
        }

        /// <summary>
        /// Plays a cue once. Spatial cues play at position, with their distances multiplied by
        /// distanceScale; others play in 2D. Does nothing without a SoundPlayer, a cue or clips.
        /// </summary>
        public static void Play(ISoundCue cue, Vector3 position, float volumeScale = 1f, float pitchScale = 1f,
            float distanceScale = 1f)
        {
            if (Instance != null)
            {
                Instance.PlayOneShot(cue, position, volumeScale, pitchScale, distanceScale);
            }
        }

        /// <summary>Plays a cue once in 2D, whatever its Spatial setting (UI sounds, chimes).</summary>
        public static void Play2D(ISoundCue cue, float volumeScale = 1f, float pitchScale = 1f)
        {
            if (Instance != null)
            {
                Instance.PlayOneShot(cue, null, volumeScale, pitchScale, 1f);
            }
        }

        /// <summary>
        /// Stops every one-shot of a cue that is playing, with a short fade (a speech the player
        /// skips). A loop is its owner's to stop. Does nothing without a SoundPlayer or a cue.
        /// </summary>
        public static void Stop(ISoundCue cue)
        {
            if (Instance != null)
            {
                Instance.StopOneShots(cue);
            }
        }

        /// <summary>
        /// Starts a cue looping (one of its clips, from a random point), fading in. Returns null
        /// without a SoundPlayer, a cue, clips or a free voice. The caller stops it when done.
        /// </summary>
        public static SoundLoop StartLoop(ISoundCue cue, Vector3 position, float distanceScale = 1f)
        {
            return Instance != null ? Instance.StartLoopVoice(cue, position, distanceScale) : null;
        }

        /// <summary>
        /// Adds a play to the counts under name. SoundPlayer counts what it plays; this is for a
        /// sound the game plays through a source of its own.
        /// </summary>
        public static void CountPlay(string name)
        {
            _playCounts[name] = _playCounts.TryGetValue(name, out int count) ? count + 1 : 1;
        }

        /// <summary>Self-test: how often each cue has played this session, most first ("Pop ×12, Splat ×3").</summary>
        public static string Report()
        {
            if (_playCounts.Count == 0)
            {
                return "none";
            }

            return string.Join(", ", _playCounts.OrderByDescending(p => p.Value).ThenBy(p => p.Key)
                .Select(p => p.Key + " ×" + p.Value));
        }

        //----------

        private void Awake()
        {
            Instance = this;

            _voices = new Voice[_voiceCount];
            for (int i = 0; i < _voices.Length; i++)
            {
                var go = new GameObject($"Voice {i + 1}");
                go.transform.SetParent(transform, false);
                var source = go.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.dopplerLevel = 0f; // the camera follows or cuts between what makes the sound; Doppler only warbles
                source.rolloffMode = AudioRolloffMode.Logarithmic;
                _voices[i] = new Voice { Source = source };
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void Update()
        {
            float deltaTime = Time.unscaledDeltaTime;
            foreach (var voice in _voices)
            {
                var cue = voice.Cue;
                if (cue == null)
                {
                    continue;
                }

                var loop = voice.Loop;
                float gain = 1f;
                float pitch = 1f;
                if (loop != null)
                {
                    float target = loop.IsStopping ? 0f : loop.Volume;
                    float step = cue.FadeTime > 0f ? deltaTime / cue.FadeTime : 1f;
                    loop.Gain = Mathf.MoveTowards(loop.Gain, target, step);

                    if (loop.IsStopping && loop.Gain <= 0f)
                    {
                        voice.Source.Stop();
                        loop.IsReleased = true;
                        voice.Loop = null;
                        voice.Cue = null;
                        continue;
                    }

                    gain = loop.Gain;
                    pitch = loop.Pitch;
                }
                else if (!voice.Source.isPlaying)
                {
                    voice.Cue = null;
                    continue;
                }
                else
                {
                    if (cue.MaxLength > 0f && voice.Source.clip != null)
                    {
                        float played = (voice.Source.timeSamples - voice.StartSample) / (float)voice.Source.clip.frequency;
                        gain = Mathf.Clamp01((cue.MaxLength - played) / CutFade);
                    }

                    // Told to stop, it fades the same way (StopTime is infinity until then).
                    gain = Mathf.Min(gain, Mathf.Clamp01(1f - (Time.unscaledTime - voice.StopTime) / CutFade));
                    if (gain <= 0f)
                    {
                        voice.Source.Stop();
                        voice.Cue = null;
                        continue;
                    }
                }

                Apply(voice, gain, pitch);
            }
        }

        //----------

        private void PlayOneShot(ISoundCue cue, Vector3? position, float volumeScale, float pitchScale, float distanceScale)
        {
            if (IsMissing(cue) || volumeScale <= 0f || cue.TakeCount == 0)
            {
                return;
            }

            var state = GetState(cue);
            float now = Time.unscaledTime;
            if (now - state.LastPlayTime < cue.Cooldown)
            {
                return;
            }

            var voice = FindVoice(cue);
            if (voice == null)
            {
                return;
            }

            state.LastPlayTime = now;
            var source = Prepare(voice, cue, state, position, distanceScale);
            source.loop = false;
            var volume = cue.Volume;
            var pitch = cue.Pitch;
            voice.BaseVolume = Random.Range(volume.x, volume.y) * voice.TakeGain * volumeScale;
            voice.BasePitch = Random.Range(pitch.x, pitch.y) * pitchScale;
            Apply(voice, 1f, 1f);
            source.Play();
            if (voice.StartSample > 0)
            {
                source.timeSamples = voice.StartSample; // where the sound starts
            }

            if (cue.DucksMusic)
            {
                _duckTime = now;
            }

            CountPlay(state.Name);
        }

        private void StopOneShots(ISoundCue cue)
        {
            if (IsMissing(cue))
            {
                return;
            }

            foreach (var voice in _voices)
            {
                if (voice.Loop == null && voice.Cue == cue)
                {
                    voice.StopTime = Mathf.Min(voice.StopTime, Time.unscaledTime);
                }
            }
        }

        private SoundLoop StartLoopVoice(ISoundCue cue, Vector3 position, float distanceScale)
        {
            if (IsMissing(cue) || cue.TakeCount == 0)
            {
                return null;
            }

            var voice = FindVoice(null); // loops don't count against a cue's one-shot voices
            if (voice == null)
            {
                return null;
            }

            var state = GetState(cue);
            var source = Prepare(voice, cue, state, position, distanceScale);
            source.loop = true;
            voice.StartSample = 0; // a loop plays from anywhere
            var volume = cue.Volume;
            var pitch = cue.Pitch;
            voice.BaseVolume = Random.Range(volume.x, volume.y) * voice.TakeGain;
            voice.BasePitch = Random.Range(pitch.x, pitch.y);

            var loop = new SoundLoop(source);
            voice.Loop = loop;
            Apply(voice, 0f, 1f);
            source.Play();
            source.time = Random.Range(0f, source.clip.length * 0.99f); // loops started together don't sound alike
            CountPlay(state.Name);
            return loop;
        }

        private AudioSource Prepare(Voice voice, ISoundCue cue, CueState state, Vector3? position, float distanceScale)
        {
            if (voice.Source.isPlaying)
            {
                voice.Source.Stop();
            }

            var take = cue.GetTake(state.NextClip(cue.TakeCount));
            var bus = cue.Bus;
            voice.Cue = cue;
            voice.Bus = bus;
            voice.StartTime = Time.unscaledTime;
            voice.StopTime = float.PositiveInfinity;
            voice.StartSample = take.Start;
            voice.TakeGain = take.Gain;

            var source = voice.Source;
            source.clip = take.Clip;
            source.outputAudioMixerGroup = OutputOf(cue, bus);
            source.priority = cue.Priority;
            source.bypassReverbZones = bus != null && bus.BypassReverbZones;
            if (bus != null && bus.LowPass && voice.Filter == null)
            {
                voice.Filter = source.gameObject.AddComponent<AudioLowPassFilter>();
            }

            float spatialBlend = position.HasValue ? cue.SpatialBlend : 0f;
            source.spatialBlend = spatialBlend;
            if (spatialBlend > 0f)
            {
                source.transform.position = position.Value;
                source.minDistance = cue.MinDistance * distanceScale;
                source.maxDistance = Mathf.Max(cue.MaxDistance * distanceScale, source.minDistance + 0.01f);
            }

            return source;
        }

        /// <summary>Sets a voice's volume, pitch and muffling from its own, its loop's and its bus's.</summary>
        private static void Apply(Voice voice, float gain, float pitch)
        {
            var bus = voice.Bus;
            voice.Source.volume = voice.BaseVolume * gain * (bus != null ? bus.Volume : 1f);
            voice.Source.pitch = voice.BasePitch * pitch * (bus != null ? bus.Pitch : 1f);
            if (voice.Filter != null)
            {
                // A voice keeps its filter once it has one; other buses' sounds pass through it open.
                voice.Filter.cutoffFrequency = bus != null && bus.LowPass ? bus.Cutoff : SoundBus.Open;
            }
        }

        private AudioMixerGroup OutputOf(ISoundCue cue, SoundBus bus)
        {
            var output = cue.Output;
            if (output == null && bus != null)
            {
                output = bus.Output;
            }

            return output != null ? output : _defaultOutput;
        }

        // An asset cue that was never assigned or has been destroyed is Unity's kind of null, which a
        // comparison through the interface doesn't see.
        private static bool IsMissing(ISoundCue cue) => cue == null || (cue is Object asset && asset == null);

        /// <summary>
        /// A voice for a new sound: the oldest of this cue's once it has Max Voices playing, else a
        /// free one, else the oldest one-shot of any cue. Null if every voice is a loop.
        /// </summary>
        private Voice FindVoice(ISoundCue cue)
        {
            Voice free = null, oldestOfCue = null, oldest = null;
            int ofCue = 0;
            foreach (var voice in _voices)
            {
                if (voice.Loop != null)
                {
                    continue;
                }

                if (!voice.Source.isPlaying)
                {
                    free ??= voice;
                    continue;
                }

                if (oldest == null || voice.StartTime < oldest.StartTime)
                {
                    oldest = voice;
                }

                if (cue != null && voice.Cue == cue)
                {
                    ofCue++;
                    if (oldestOfCue == null || voice.StartTime < oldestOfCue.StartTime)
                    {
                        oldestOfCue = voice;
                    }
                }
            }

            if (cue != null && ofCue >= cue.MaxVoices)
            {
                return oldestOfCue;
            }

            return free ?? oldest;
        }

        private CueState GetState(ISoundCue cue)
        {
            if (!_cueStates.TryGetValue(cue, out var state))
            {
                state = new CueState { Name = cue.Name };
                _cueStates.Add(cue, state);
            }

            return state;
        }

        //----------

        private sealed class Voice
        {
            public AudioSource Source;
            public AudioLowPassFilter Filter; // added the first time the voice plays on a bus with one
            public ISoundCue Cue; // null once its one-shot has ended
            public SoundBus Bus;
            public float StartTime;
            public float StopTime = float.PositiveInfinity; // when Stop named its cue
            public int StartSample; // the sample the clip plays from
            public float TakeGain = 1f;
            public float BaseVolume;
            public float BasePitch = 1f;
            public SoundLoop Loop; // non-null while a loop owns this voice
        }

        /// <summary>A cue's runtime state: its shuffled deck of clip indices, and when it last played.</summary>
        private sealed class CueState
        {
            private int[] _deck = new int[0];
            private int _next;

            public string Name; // read once: an asset's name is a new string every time
            public float LastPlayTime = float.NegativeInfinity;

            /// <summary>The next clip from the deck, reshuffling when it runs out (or the cue's clips change).</summary>
            public int NextClip(int clipCount)
            {
                if (clipCount <= 1)
                {
                    return 0;
                }

                if (_deck.Length != clipCount)
                {
                    _deck = new int[clipCount];
                    for (int i = 0; i < clipCount; i++)
                    {
                        _deck[i] = i;
                    }

                    _next = clipCount; // shuffle below
                }

                if (_next >= _deck.Length)
                {
                    int last = _deck[_deck.Length - 1];
                    for (int i = _deck.Length - 1; i > 0; i--)
                    {
                        int j = Random.Range(0, i + 1);
                        (_deck[i], _deck[j]) = (_deck[j], _deck[i]);
                    }

                    // The new deck mustn't start with the clip the old one ended on.
                    if (_deck[0] == last)
                    {
                        int j = Random.Range(1, _deck.Length);
                        (_deck[0], _deck[j]) = (_deck[j], _deck[0]);
                    }

                    _next = 0;
                }

                return _deck[_next++];
            }
        }
    }
}
