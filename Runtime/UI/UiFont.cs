using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;

namespace Aurumworks
{
    /// <summary>
    /// One typeface of a theme: a TextMeshPro font asset, or a font file made into a dynamic one when
    /// the game starts (so a game needs no font atlas on disk). A slot with neither, or a file that
    /// can't be made, gives TextMeshPro's default font, and a made font falls back to it for
    /// characters it lacks.
    /// </summary>
    [Serializable]
    public class UiFont
    {
        [Tooltip("A TextMeshPro font asset. Empty: one is made from Source when the game starts.")]
        [SerializeField] private TMP_FontAsset _asset;
        [Tooltip("A font file (TTF or OTF), used when Asset is empty.")]
        [SerializeField] private Font _source;
        [Tooltip("Off: text in this face is set without kerning. TextMeshPro misreads some files' " +
                 "pair tables when it makes a font at runtime (a letter swallows the space after it).")]
        [SerializeField] private bool _kerning = true;

        // Made fonts last one session. Kept here, not in the slot: a theme asset outlives play mode.
        private static readonly Dictionary<Font, TMP_FontAsset> _made = new();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => _made.Clear();

        /// <summary>The font to set text in; never null once TextMeshPro has a default font.</summary>
        public TMP_FontAsset Asset => _asset != null ? _asset : Make(_source);

        /// <summary>True if the face is made from a font file this session.</summary>
        public bool IsMade => _asset == null && _source != null && Make(_source) != TMP_Settings.defaultFontAsset;

        public bool Kerning => _kerning;

        /// <summary>
        /// A dynamic TextMeshPro font for a font file, made once per session; the default font for
        /// null, or when it can't be made.
        /// </summary>
        public static TMP_FontAsset Make(Font source)
        {
            TMP_FontAsset fallback = TMP_Settings.defaultFontAsset;
            if (source == null)
            {
                return fallback;
            }

            if (_made.TryGetValue(source, out var made) && made != null)
            {
                return made;
            }

            // Windowless runs: a font made at runtime gets no material without a graphics device,
            // and nothing is seen.
            TMP_FontAsset asset = SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null
                ? null
                : TMP_FontAsset.CreateFontAsset(source);
            if (asset == null || asset.material == null)
            {
                asset = fallback;
            }
            else
            {
                asset.name = source.name;
                if (fallback != null)
                {
                    asset.fallbackFontAssetTable = new List<TMP_FontAsset> { fallback };
                }
            }

            _made[source] = asset;
            return asset;
        }
    }
}
