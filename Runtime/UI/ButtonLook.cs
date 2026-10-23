using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Aurumworks
{
    /// <summary>
    /// How a button or tile shows its state, the same on every screen. Under the pointer it brightens
    /// (a tint can only darken, so it rests a little dim) and a framed one's frame takes the accent
    /// colour. Keyboard or gamepad focus is a thin outline round it, so a screen's main action, which
    /// starts with the focus, still brightens when hovered. A control switched off itself looks it;
    /// one switched off by a CanvasGroup above it, as everything under an open menu is, keeps the
    /// look it has at rest.
    /// </summary>
    public class ButtonLook : MonoBehaviour, ISelectHandler, IDeselectHandler, IPointerEnterHandler, IPointerExitHandler,
        IPointerDownHandler, IPointerUpHandler
    {
        private const float FadeTime = 0.08f;
        private const float FocusMargin = 5f;

        private static readonly Color _pressedTint = new(0.7f, 0.7f, 0.7f, 1f);

        private Color? _idle, _lit;
        private bool _selected, _hovered, _pressed, _started;
        private bool? _usable;

        public Selectable Control { get; set; }

        /// <summary>The shape that brightens.</summary>
        public Graphic Fill { get; set; }

        /// <summary>A frame that takes the Lit colour under the pointer, or null.</summary>
        public Graphic Frame { get; set; }

        /// <summary>The frame's colour at rest; the theme's line colour unless set.</summary>
        public Color Idle
        {
            get => _idle ?? UiKit.Theme.Line;
            set => _idle = value;
        }

        /// <summary>The frame's colour under the pointer; the theme's accent unless set.</summary>
        public Color Lit
        {
            get => _lit ?? UiKit.Theme.Accent;
            set => _lit = value;
        }

        /// <summary>The outline shown while the control has the keyboard's focus.</summary>
        public Graphic Focus { get; set; }

        /// <summary>Something shown only under the pointer (a tile's picture lifts), or null.</summary>
        public Graphic Glow { get; set; }

        /// <summary>The fill's tint at rest.</summary>
        public Color Rest { get; set; } = new(0.86f, 0.86f, 0.86f, 1f);

        /// <summary>The fill's tint while the control can't be used.</summary>
        public Color Disabled { get; set; } = new(0.55f, 0.55f, 0.55f, 0.7f);

        /// <summary>
        /// Gives a control the look: fill brightens, and frame (or null) takes the accent colour.
        /// corner is the control's own, which the focus outline follows.
        /// </summary>
        public static ButtonLook On(Selectable control, Graphic fill, Graphic frame, float corner)
        {
            control.transition = Selectable.Transition.None;
            var look = control.gameObject.AddComponent<ButtonLook>();
            look.Control = control;
            look.Fill = fill;
            look.Frame = frame;

            RectTransform outline = UiKit.Anchor(UiKit.Rect("Focus", control.transform), 0f, 0f, 1f, 1f);
            outline.offsetMin = new Vector2(-FocusMargin, -FocusMargin);
            outline.offsetMax = new Vector2(FocusMargin, FocusMargin);
            look.Focus = UiKit.Shape(outline, UiKit.Theme.Text, corner + 3f, UiKit.Theme.FrameWidth);
            outline.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            look.Focus.enabled = false;
            return look;
        }

        //----------

        private void Start()
        {
            _started = true;
            Apply();
        }

        private void OnDisable() => _hovered = _pressed = false;

        private void Update()
        {
            if (Control != null && _usable != Control.interactable)
            {
                Apply();
            }
        }

        //----------

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

        public void OnPointerDown(PointerEventData eventData)
        {
            _pressed = true;
            Apply();
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            _pressed = false;
            Apply();
        }

        private void Apply()
        {
            if (Control == null)
            {
                return;
            }

            bool usable = Control.interactable;
            bool instant = !_started || _usable != usable;
            _usable = usable;
            if (Fill != null)
            {
                Color tint = !usable ? Disabled : _pressed ? _pressedTint : _hovered ? Color.white : Rest;
                Fill.CrossFadeColor(tint, instant ? 0f : FadeTime, true, true);
            }

            if (Frame != null)
            {
                Frame.color = usable && _hovered ? Lit : Idle;
            }

            if (Focus != null)
            {
                Focus.enabled = usable && _selected;
            }

            if (Glow != null)
            {
                Glow.enabled = usable && _hovered;
            }
        }
    }
}
