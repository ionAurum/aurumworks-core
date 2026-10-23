using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Aurumworks
{
    /// <summary>
    /// Lights a card's frame in the accent colour while it is hovered or selected (keyboard and
    /// gamepad).
    /// </summary>
    public class FrameHighlight : MonoBehaviour, ISelectHandler, IDeselectHandler, IPointerEnterHandler, IPointerExitHandler
    {
        private Color? _idle, _lit;
        private bool _selected, _hovered;

        public Graphic Frame { get; set; }

        /// <summary>The frame's colour at rest; the theme's line colour unless set.</summary>
        public Color Idle
        {
            get => _idle ?? UiKit.Theme.Line;
            set => _idle = value;
        }

        /// <summary>The frame's colour while hovered or selected; the theme's accent unless set.</summary>
        public Color Lit
        {
            get => _lit ?? UiKit.Theme.Accent;
            set => _lit = value;
        }

        private void Start() => Apply();

        public void OnSelect(BaseEventData eventData)
        {
            _selected = true;
            Apply();
        }

        public void OnDeselect(BaseEventData eventData)
        {
            _selected = false;
            Apply();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            _hovered = true;
            Apply();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _hovered = false;
            Apply();
        }

        private void Apply()
        {
            if (Frame != null)
            {
                Frame.color = _selected || _hovered ? Lit : Idle;
            }
        }
    }
}
