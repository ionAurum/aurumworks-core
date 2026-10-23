using System;
using System.Linq;
using UnityEngine;

namespace Aurumworks
{
    /// <summary>
    /// Measures a cue's clips as they load, so the files needn't be trimmed or levelled by hand: a
    /// one-shot starts where its sound does, skipping silence before it, and every variation is
    /// brought to one loudness (a louder one turned down, a quieter one played from a raised copy
    /// whose sharpest peaks are rounded off). The files are never changed. Only for clips whose
    /// samples can be read (Decompress On Load); others play as they are.
    /// </summary>
    public static class SoundLevelling
    {
        // A raised copy: its peaks stay under -1 dBFS, it comes up by at most 18 dB, and by at most
        // 12 dB more than its peak has room for (the limiter rounds off what would pass; more than
        // that is heard).
        private const float MaxPeak = 0.89f;
        private const float MaxBoost = 7.94f;
        private const float IntoLimiter = 4f;
        private const float Knee = 0.6f;

        /// <summary>
        /// Where each clip's sound starts (the first sample within 40 dB of its peak, less 5 ms;
        /// only when trim is set) and its loudness (the mean power of its 20 ms windows within 40 dB
        /// of its peak). The cue's level is level (dBFS), or without one its median clip's loudness.
        /// Each louder clip is turned down to it; each quieter one is replaced by a raised copy, up
        /// to that level or 18 dB, whichever comes first.
        /// </summary>
        public static SoundTake[] Measure(AudioClip[] clips, bool trim, float? level)
        {
            var takeClips = (AudioClip[])clips.Clone();
            var starts = new int[clips.Length];
            var gains = new float[clips.Length];
            var boosts = new float[clips.Length];
            var loudness = new float?[clips.Length];
            var samples = new float[clips.Length][];
            var peaks = new float[clips.Length];

            for (int t = 0; t < clips.Length; t++)
            {
                gains[t] = 1f;
                boosts[t] = 1f;

                var clip = clips[t];
                if (clip.loadState != AudioDataLoadState.Loaded)
                {
                    clip.LoadAudioData();
                }

                var data = new float[clip.samples * clip.channels];
                if (data.Length == 0 || !clip.GetData(data, 0))
                {
                    continue;
                }

                int channels = clip.channels;
                int frames = clip.samples;
                float peak = 0f;
                foreach (float value in data)
                {
                    peak = Mathf.Max(peak, Mathf.Abs(value));
                }

                if (peak <= 1e-4f)
                {
                    continue;
                }

                samples[t] = data;
                peaks[t] = peak;
                float gate = Mathf.Max(peak * 0.01f, 0.003f);
                if (trim)
                {
                    int first = 0;
                    while (first < data.Length && Mathf.Abs(data[first]) < gate)
                    {
                        first++;
                    }

                    starts[t] = Mathf.Clamp(first / channels - clip.frequency / 200, 0, frames - 1);
                }

                int window = Mathf.Max(1, clip.frequency / 50);
                double power = 0;
                int counted = 0;
                for (int w = 0; w + window <= frames; w += window)
                {
                    double sum = 0;
                    for (int i = w * channels; i < (w + window) * channels; i++)
                    {
                        sum += data[i] * data[i];
                    }

                    double mean = sum / (window * channels);
                    if (mean > gate * gate * 0.25)
                    {
                        power += mean;
                        counted++;
                    }
                }

                if (counted > 0)
                {
                    loudness[t] = (float)Math.Sqrt(power / counted);
                }
            }

            float[] known = loudness.Where(l => l.HasValue).Select(l => l.Value).OrderBy(l => l).ToArray();
            if (known.Length > 1 || (level.HasValue && known.Length > 0))
            {
                float target = level.HasValue ? Mathf.Pow(10f, level.Value / 20f) : known[known.Length / 2];
                for (int t = 0; t < clips.Length; t++)
                {
                    if (!loudness[t].HasValue)
                    {
                        continue;
                    }

                    if (loudness[t] > target)
                    {
                        gains[t] = target / loudness[t].Value;
                        continue;
                    }

                    // A quieter one is raised in a louder copy of its own: toward the level, by at
                    // most 18 dB, and no further into the limiter than 12 dB.
                    float boost = Mathf.Min(target / loudness[t].Value, MaxPeak / peaks[t] * IntoLimiter, MaxBoost);
                    if (boost < 1.06f)
                    {
                        continue;
                    }

                    var quiet = clips[t];
                    Raise(samples[t], boost);
                    var loud = AudioClip.Create(quiet.name, quiet.samples, quiet.channels, quiet.frequency, false);
                    loud.SetData(samples[t], 0);
                    takeClips[t] = loud;
                    boosts[t] = boost;
                }
            }

            var takes = new SoundTake[clips.Length];
            for (int t = 0; t < takes.Length; t++)
            {
                takes[t] = new SoundTake(takeClips[t], starts[t], gains[t], boosts[t]);
            }

            return takes;
        }

        /// <summary>
        /// Makes data louder by boost. What would pass -1 dBFS is rounded off (a soft limiter), so a
        /// quiet file with a few sharp peaks can still come up.
        /// </summary>
        private static void Raise(float[] data, float boost)
        {
            const float room = MaxPeak - Knee;
            for (int i = 0; i < data.Length; i++)
            {
                float value = data[i] * boost;
                float size = Mathf.Abs(value);
                data[i] = size <= Knee ? value : Mathf.Sign(value) * (Knee + room * (float)Math.Tanh((size - Knee) / room));
            }
        }
    }
}
