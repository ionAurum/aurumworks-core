using TMPro;
using UnityEngine;

namespace Aurumworks
{
    /// <summary>
    /// A game's look, as the UI kit draws it: colours, typefaces, the shape of panels and buttons, how
    /// controls move, and their sounds. The kit's layout and behaviour are the same in every game;
    /// this asset is what differs. A game hands its theme to UiKit.Theme as it starts.
    /// </summary>
    [CreateAssetMenu(fileName = "UI Theme", menuName = "Aurumworks/UI Theme")]
    public class UiTheme : ScriptableObject
    {
        [Header("Colours")]
        [Tooltip("Behind everything; also the inside of sliders and text fields.")]
        [SerializeField] private Color _background = new Color32(0x12, 0x14, 0x18, 0xff);
        [Tooltip("Panels and dialogs.")]
        [SerializeField] private Color _panel = new Color32(0x1c, 0x20, 0x27, 0xff);
        [Tooltip("Buttons, and anything else that stands on a panel.")]
        [SerializeField] private Color _raised = new Color32(0x28, 0x2e, 0x38, 0xff);
        [Tooltip("Frames and dividers.")]
        [SerializeField] private Color _line = new Color32(0x3c, 0x45, 0x54, 0xff);
        [SerializeField] private Color _text = new Color32(0xee, 0xee, 0xee, 0xff);
        [Tooltip("Quieter text: labels, hints, the status line.")]
        [SerializeField] private Color _muted = new Color32(0x94, 0x9c, 0xa8, 0xff);
        [Tooltip("The main action of a screen, titles, a slider's handle, a hovered frame.")]
        [SerializeField] private Color _accent = new Color32(0x4c, 0xa6, 0xe8, 0xff);
        [Tooltip("The accent, held back: a slider's fill, a dialog's frame.")]
        [SerializeField] private Color _accentDim = new Color32(0x2c, 0x5c, 0x80, 0xff);
        [Tooltip("Text on an accent button.")]
        [SerializeField] private Color _onAccent = new Color32(0x08, 0x12, 0x1a, 0xff);
        [Tooltip("A button that ends or deletes something.")]
        [SerializeField] private Color _danger = new Color32(0x8e, 0x2a, 0x23, 0xff);

        [Header("Fonts")]
        [Tooltip("Titles and the main action's label.")]
        [SerializeField] private UiFont _display = new UiFont();
        [Tooltip("Running text.")]
        [SerializeField] private UiFont _body = new UiFont();
        [Tooltip("Button labels and emphasis. Bold text in the body font uses it when both are made from font files.")]
        [SerializeField] private UiFont _bodyBold = new UiFont();
        [Tooltip("Titles and the main action's label are set in capitals.")]
        [SerializeField] private bool _upperCaseTitles = true;
        [Tooltip("Every button is lettered in the display face, as the main action is; off: the others in the bold body face.")]
        [SerializeField] private bool _displayButtons;

        [Header("Shape")]
        [Tooltip("Cut: corners cut off at 45°. Round: rounded corners.")]
        [SerializeField] private UiCornerStyle _cornerStyle = UiCornerStyle.Round;
        [Tooltip("Which corners are cut or rounded; the others stay square.")]
        [SerializeField] private UiCorners _corners = UiCorners.All;
        [Tooltip("Corner size of panels and dialogs, in reference pixels (1920 × 1080).")]
        [SerializeField] private float _panelCorner = 16f;
        [SerializeField] private float _buttonCorner = 10f;
        [Tooltip("Corner size of small parts: a slider's groove and handle.")]
        [SerializeField] private float _smallCorner = 4f;
        [Tooltip("Width of frames and of the keyboard focus outline.")]
        [SerializeField] private float _frameWidth = 2f;

        [Header("Motion")]
        [Tooltip("A control's scale while hovered or selected (1 = it doesn't swell).")]
        [SerializeField] private float _hoverScale = 1f;
        [Tooltip("While pressed: this much wider and this much shorter, as a fraction of the size (0 = none).")]
        [SerializeField] private float _pressSquash = 0f;
        [Tooltip("Keyboard or gamepad Submit: the squash's starting speed.")]
        [SerializeField] private float _submitKick = 0f;
        [Tooltip("Spring stiffness: higher is snappier.")]
        [SerializeField] private float _stiffness = 300f;
        [Tooltip("Lower wobbles longer. No overshoot at 2 × √Stiffness (about 35 at 300).")]
        [SerializeField] private float _damping = 14f;
        [Tooltip("The main menu's title squashes and stretches on its own by this fraction of its size (0 = still).")]
        [SerializeField] private float _titleSquash = 0f;

        [Header("Sounds")]
        [Tooltip("When the pointer enters a control, or navigation selects it.")]
        [SerializeField] private SoundCue _hoverSound;
        [Tooltip("When a control is pressed or submitted.")]
        [SerializeField] private SoundCue _pressSound;

        // A game whose cues are built in code sets these as it starts.
        private ISoundCue _hoverSoundInCode, _pressSoundInCode;

        public Color Background => _background;
        public Color Panel => _panel;
        public Color Raised => _raised;
        public Color Line => _line;
        public Color Text => _text;
        public Color Muted => _muted;
        public Color Accent => _accent;
        public Color AccentDim => _accentDim;
        public Color OnAccent => _onAccent;
        public Color Danger => _danger;

        /// <summary>Titles and the main action's label.</summary>
        public TMP_FontAsset Display => _display.Asset;

        /// <summary>Running text.</summary>
        public TMP_FontAsset Body
        {
            get
            {
                LinkBold();
                return _body.Asset;
            }
        }

        /// <summary>Button labels and emphasis.</summary>
        public TMP_FontAsset BodyBold => _bodyBold.Asset;

        public bool UpperCaseTitles => _upperCaseTitles;

        /// <summary>True if every button is lettered in the display face, not only the main action.</summary>
        public bool DisplayButtons => _displayButtons;

        public UiCornerStyle CornerStyle => _cornerStyle;
        public UiCorners Corners => _corners;
        public float PanelCorner => _panelCorner;
        public float ButtonCorner => _buttonCorner;
        public float SmallCorner => _smallCorner;
        public float FrameWidth => _frameWidth;

        /// <summary>True if controls swell, squash or kick at all.</summary>
        public bool HasMotion => _hoverScale != 1f || _pressSquash != 0f || _submitKick != 0f;

        public float HoverScale => _hoverScale;
        public float PressSquash => _pressSquash;
        public float SubmitKick => _submitKick;
        public float Stiffness => _stiffness;
        public float Damping => _damping;
        public float TitleSquash => _titleSquash;

        /// <summary>The asset's cue, or the one a game set from code.</summary>
        public ISoundCue HoverSound
        {
            get => _hoverSoundInCode ?? (_hoverSound != null ? _hoverSound : null);
            set => _hoverSoundInCode = value;
        }

        /// <summary>The asset's cue, or the one a game set from code.</summary>
        public ISoundCue PressSound
        {
            get => _pressSoundInCode ?? (_pressSound != null ? _pressSound : null);
            set => _pressSoundInCode = value;
        }

        /// <summary>False for a face the theme sets without kerning (UiFont's Kerning box).</summary>
        public bool HasKerning(TMP_FontAsset font)
        {
            return !(Matches(_display, font) || Matches(_body, font) || Matches(_bodyBold, font));
        }

        private static bool Matches(UiFont slot, TMP_FontAsset font) => !slot.Kerning && slot.Asset == font;

        // <b> and FontStyles.Bold in the body face use the real bold face. Only for faces made this
        // session: a font asset on disk keeps its own weight table.
        private void LinkBold()
        {
            if (!_body.IsMade || !_bodyBold.IsMade)
            {
                return;
            }

            TMP_FontAsset body = _body.Asset;
            TMP_FontAsset bold = _bodyBold.Asset;
            TMP_FontWeightPair[] weights = body.fontWeightTable; // the asset's own table, so changing an entry sticks
            if (weights != null && weights.Length > 7 && weights[7].regularTypeface != bold)
            {
                weights[7].regularTypeface = bold;
            }
        }
    }
}
