using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Aurumworks
{
    /// <summary>
    /// Menu juice: makes a UI element wobble like jelly. It swells while hovered or selected, squashes
    /// while pressed (or with a kick on keyboard and gamepad Submit), and springs back with a little
    /// overshoot. Titles can squash and stretch on their own with Idle Squash, and code can make it
    /// pop (Pop), as the HUD does at size milestones. On a Selectable (Button, Dropdown, Input Field)
    /// it stays still while that isn't interactable. It only changes the
    /// localScale, which layout groups ignore while their Child Scale boxes are off, so neighbours
    /// never move. Hover and press need a raycast target on this object, which Selectables have.
    /// The UI kit adds one to the controls it builds, with the theme's motion.
    /// </summary>
    [DisallowMultipleComponent]
    public class Squishy : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler,
        IPointerUpHandler, ISelectHandler, IDeselectHandler, ISubmitHandler
    {
        private const float MaxStep = 1f / 30f; // seconds: keeps the springs stable through frame hitches
        private const float RestEpsilon = 1e-4f;

        [Tooltip("Scale while hovered or selected (1 = normal size).")]
        [SerializeField] private float _hoverScale = 1.08f;
        [Tooltip("While pressed: this much wider and this much shorter, as a fraction of the size.")]
        [SerializeField] private float _pressSquash = 0.12f;
        [Tooltip("Keyboard or gamepad Submit: the squash's starting speed. The spring does the rest.")]
        [SerializeField] private float _submitKick = 3f;
        [Tooltip("Spring stiffness: higher is snappier.")]
        [SerializeField] private float _stiffness = 300f;
        [Tooltip("Lower wobbles longer. No overshoot at 2 × √Stiffness (about 35 at 300).")]
        [SerializeField] private float _damping = 14f;

        [Header("Idle")]
        [Tooltip("Squash and stretch on its own, for titles (0 = off).")]
        [SerializeField] private float _idleSquash = 0f;
        [Tooltip("Idle cycles per second.")]
        [SerializeField] private float _idleSpeed = 0.5f;

        private Selectable _selectable;
        private Vector3 _restScale;
        private bool _hovered, _selected, _pressed;
        private float _size = 1f, _sizeVelocity;
        private float _squash, _squashVelocity; // positive: wider and shorter; negative: narrower and taller

        private bool IsInteractable => _selectable == null || _selectable.IsInteractable();

        /// <summary>How far it squashes and stretches on its own, as a fraction of its size (0 = still).</summary>
        public float IdleSquash
        {
            get => _idleSquash;
            set => _idleSquash = value;
        }

        /// <summary>Adds one to a control built in code, with a theme's motion.</summary>
        public static Squishy On(GameObject control, UiTheme theme)
        {
            var squishy = control.AddComponent<Squishy>();
            squishy._hoverScale = theme.HoverScale;
            squishy._pressSquash = theme.PressSquash;
            squishy._submitKick = theme.SubmitKick;
            squishy._stiffness = theme.Stiffness;
            squishy._damping = theme.Damping;
            return squishy;
        }

        //----------

        private void Awake()
        {
            _selectable = GetComponent<Selectable>();
            _restScale = transform.localScale;
        }

        // A menu hidden mid-wobble (or while hovered) comes back at rest.
        private void OnDisable()
        {
            _hovered = _selected = _pressed = false;
            _size = 1f;
            _squash = _sizeVelocity = _squashVelocity = 0f;
            transform.localScale = _restScale;
        }

        private void Update()
        {
            float deltaTime = Mathf.Min(Time.unscaledDeltaTime, MaxStep);
            bool interactable = IsInteractable;

            float sizeTarget = interactable && (_hovered || _selected) ? _hoverScale : 1f;
            float squashTarget = interactable && _pressed ? _pressSquash : 0f;
            if (_idleSquash > 0f)
            {
                squashTarget += _idleSquash * Mathf.Sin(Time.unscaledTime * _idleSpeed * 2f * Mathf.PI);
            }

            // Moving a UI element makes its canvas rebuild, so leave the scale alone once at rest.
            if (IsAtRest(_size, _sizeVelocity, sizeTarget) && IsAtRest(_squash, _squashVelocity, squashTarget))
            {
                return;
            }

            Spring(ref _size, ref _sizeVelocity, sizeTarget, deltaTime);
            Spring(ref _squash, ref _squashVelocity, squashTarget, deltaTime);
            transform.localScale = Vector3.Scale(_restScale, new Vector3(_size * (1f + _squash), _size * (1f - _squash), 1f));
        }

        //----------

        public void OnPointerEnter(PointerEventData eventData) => _hovered = true;

        public void OnPointerExit(PointerEventData eventData)
        {
            _hovered = false;
            _pressed = false;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left)
            {
                _pressed = true;
            }
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left)
            {
                _pressed = false;
            }
        }

        public void OnSelect(BaseEventData eventData) => _selected = true;

        public void OnDeselect(BaseEventData eventData) => _selected = false;

        public void OnSubmit(BaseEventData eventData)
        {
            if (IsInteractable)
            {
                _squashVelocity += _submitKick;
            }
        }

        /// <summary>
        /// Kicks the size spring: it swells by about speed ÷ √Stiffness and wobbles back to rest.
        /// For HUD juice, such as the size readout at a milestone.
        /// </summary>
        public void Pop(float speed) => _sizeVelocity += speed;

        //----------

        // Semi-implicit Euler: stable while deltaTime < 2 / √stiffness (0.115 s at 300).
        private void Spring(ref float value, ref float velocity, float target, float deltaTime)
        {
            velocity += (_stiffness * (target - value) - _damping * velocity) * deltaTime;
            value += velocity * deltaTime;
        }

        private static bool IsAtRest(float value, float velocity, float target) =>
            Mathf.Abs(value - target) < RestEpsilon && Mathf.Abs(velocity) < RestEpsilon;
    }
}
