using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Aurumworks
{
    /// <summary>
    /// UI sounds for a control, in 2D: Hover when the pointer enters it or keyboard or gamepad
    /// navigation selects it (Squishy's swell), Press when it's pressed or submitted (Squishy's squash).
    /// Silent while its Selectable isn't interactable, and for a moment after it appears: a screen
    /// rebuilt under a resting pointer, or a menu selecting its first control, isn't a hover. Each sound is the cue set in the Inspector, or
    /// the one set from code, or else the theme's.
    /// </summary>
    public class UiSound : MonoBehaviour, IPointerEnterHandler, ISelectHandler, IPointerDownHandler, ISubmitHandler
    {
        // A menu selects its first control as it opens, and a control built under a resting pointer
        // is entered at once; neither should sound like the player moving.
        private const float SelectGrace = 0.1f;

        [Tooltip("Empty: the theme's hover sound.")]
        [SerializeField] private SoundCue _hover;
        [Tooltip("Empty: the theme's press sound.")]
        [SerializeField] private SoundCue _press;

        private ISoundCue _hoverInCode, _pressInCode;
        private Selectable _selectable;
        private float _enabledTime;

        public ISoundCue Hover
        {
            get => _hoverInCode ?? (_hover != null ? _hover : UiKit.Theme.HoverSound);
            set => _hoverInCode = value;
        }

        public ISoundCue Press
        {
            get => _pressInCode ?? (_press != null ? _press : UiKit.Theme.PressSound);
            set => _pressInCode = value;
        }

        /// <summary>Gives a control its sounds, if it has none yet; returns them, to change a cue.</summary>
        public static UiSound On(Component control)
        {
            var sound = control.GetComponent<UiSound>();
            return sound != null ? sound : control.gameObject.AddComponent<UiSound>(); // not ??: Unity's fake null
        }

        private void Awake() => _selectable = GetComponent<Selectable>();

        private void OnEnable() => _enabledTime = Time.unscaledTime;

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (Time.unscaledTime - _enabledTime >= SelectGrace)
            {
                Play(Hover);
            }
        }

        public void OnSelect(BaseEventData eventData)
        {
            // Clicking selects too, right after the press: only navigation should sound.
            if (eventData is PointerEventData || Time.unscaledTime - _enabledTime < SelectGrace)
            {
                return;
            }

            Play(Hover);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left)
            {
                Play(Press);
            }
        }

        public void OnSubmit(BaseEventData eventData) => Play(Press);

        private void Play(ISoundCue cue)
        {
            if (_selectable == null || _selectable.IsInteractable())
            {
                SoundPlayer.Play2D(cue);
            }
        }
    }
}
