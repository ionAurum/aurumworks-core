using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Aurumworks
{
    /// <summary>
    /// What every game's main menu has round its own choices: the game's title at the top, and
    /// along the bottom a row of small buttons (Settings, Quit) with the status line and the build's
    /// version at its other end. The status line is where a game says why the last session ended. The
    /// game puts its choices between the two: in the column under the title (Choices), or anywhere
    /// on the screen if it lays them out itself. The title and the column are at the left, or where
    /// the game asks for them (Layout).
    /// </summary>
    public sealed class MainMenuFrame
    {
        /// <summary>Where a main menu's title and column of choices are.</summary>
        public enum Layout
        {
            /// <summary>At the left, with the bottom row's buttons at the left and the version at the right.</summary>
            Left,

            /// <summary>In the middle, with the bottom row as it is at the left.</summary>
            Centred,

            /// <summary>At the right, with the bottom row's buttons at the right and the version at the left.</summary>
            Right,
        }

        private const float Margin = 0.04f; // of the screen's width, at each side
        private const float ChoicesWidth = 0.33f; // of the screen's

        private readonly RectTransform _screen, _foot;
        private readonly Layout _layout;
        private readonly TextMeshProUGUI _status;
        private RectTransform _choices;

        private MainMenuFrame(RectTransform screen, string title, Layout layout)
        {
            _screen = screen;
            _layout = layout;
            Title = UiKit.Title(screen, title, 128f);
            UiKit.Anchor(Title.rectTransform, layout == Layout.Right ? 0.3f : Margin, 0.8f,
                layout == Layout.Left ? 0.7f : 1f - Margin, 0.97f);
            Title.alignment = layout switch
            {
                Layout.Centred => TextAlignmentOptions.Bottom,
                Layout.Right => TextAlignmentOptions.BottomRight,
                _ => TextAlignmentOptions.BottomLeft,
            };
            if (UiKit.Theme.TitleSquash > 0f)
            {
                // So the lettering's foot stays where it is.
                Title.rectTransform.pivot = new Vector2(layout switch { Layout.Centred => 0.5f, Layout.Right => 1f, _ => 0f }, 0f);
                Squishy.On(Title.gameObject, UiKit.Theme).IdleSquash = UiKit.Theme.TitleSquash;
            }

            _foot = UiKit.Anchor(UiKit.Rect("Foot", screen), Margin, 0.03f, 1f - Margin, 0.11f);
            var row = _foot.gameObject.AddComponent<HorizontalLayoutGroup>();
            row.spacing = 14f;
            row.childAlignment = TextAnchor.MiddleLeft;
            row.childControlWidth = row.childControlHeight = true;
            row.childForceExpandWidth = row.childForceExpandHeight = false;
            _status = UiKit.Text(_foot, BuildInfo.Version, 20f, UiKit.Theme.Muted,
                align: layout == Layout.Right ? TextAlignmentOptions.MidlineLeft : TextAlignmentOptions.MidlineRight);
            _status.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
        }

        /// <summary>The title's text, for a game that sets it in more than one colour.</summary>
        public TextMeshProUGUI Title { get; }

        /// <summary>
        /// A column under the title, for a game whose choices are a list of controls; made when
        /// first asked for. It comes before the bottom row, so the game's main action is
        /// found before Settings and Quit.
        /// </summary>
        public RectTransform Choices
        {
            get
            {
                if (_choices == null)
                {
                    float left = _layout switch
                    {
                        Layout.Centred => (1f - ChoicesWidth) / 2f,
                        Layout.Right => 1f - Margin - ChoicesWidth,
                        _ => Margin,
                    };
                    _choices = UiKit.Anchor(UiKit.Rect("Choices", _screen), left, 0.15f, left + ChoicesWidth, 0.78f);
                    _choices.SetSiblingIndex(_foot.GetSiblingIndex());
                    var column = _choices.gameObject.AddComponent<VerticalLayoutGroup>();
                    column.spacing = 16f;
                    column.childAlignment = TextAnchor.UpperLeft;
                    column.childControlWidth = column.childControlHeight = true;
                    column.childForceExpandWidth = true;
                    column.childForceExpandHeight = false;
                }

                return _choices;
            }
        }

        /// <summary>
        /// Draws the frame on a main menu's screen, with the title and the column of choices at the
        /// left, or where the layout says.
        /// </summary>
        public static MainMenuFrame On(RectTransform screen, string title, Layout layout = Layout.Left) =>
            new MainMenuFrame(screen, title, layout);

        /// <summary>Adds a button to the bottom row, after those already there.</summary>
        public Button FootButton(string label, Action pressed, float width)
        {
            Button button = UiKit.Button(_foot, label, pressed, 52f, fontSize: 20f);
            button.GetComponent<LayoutElement>().preferredWidth = width;
            if (_layout != Layout.Right)
            {
                _status.transform.SetAsLastSibling(); // the buttons, then the status line
            }

            return button;
        }

        /// <summary>Shows a status line beside the version; null or empty for the version alone.</summary>
        public void SetStatus(string status)
        {
            string line = string.IsNullOrEmpty(status) ? BuildInfo.Version : status + "  ·  " + BuildInfo.Version;
            if (_status.text != line)
            {
                _status.text = line;
            }
        }
    }
}
