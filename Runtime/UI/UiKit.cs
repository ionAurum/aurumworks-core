using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.TextCore;
using UnityEngine.UI;

namespace Aurumworks
{
    /// <summary>
    /// uGUI builders, so a screen can be made in code without prefabs or sprite art. What they build
    /// is laid out and behaves the same in every game; how it looks comes from the game's UiTheme.
    /// Sizes are in reference pixels: the canvas scales a 1920 × 1080 layout to the window.
    /// </summary>
    public static class UiKit
    {
        private const float DropdownHeight = 56f, DropdownItemHeight = 48f;

        private static UiTheme _theme;

        /// <summary>
        /// The look everything is built with. A game sets its theme as it starts, before it builds
        /// anything; without one, a plain default.
        /// </summary>
        public static UiTheme Theme
        {
            get
            {
                if (_theme == null)
                {
                    _theme = ScriptableObject.CreateInstance<UiTheme>();
                    _theme.name = "Default UI Theme";
                }

                return _theme;
            }
            set => _theme = value;
        }

        // Statics survive play mode when domain reload is disabled; reset them.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => _theme = null;

        //---------- Canvas and rects

        /// <summary>A screen-space canvas scaled from 1920 × 1080, and an EventSystem if the scene has none.</summary>
        public static Canvas CreateCanvas()
        {
            var go = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            if (UnityEngine.Object.FindAnyObjectByType<EventSystem>() == null)
            {
                _ = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            }

            return canvas;
        }

        public static RectTransform Rect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        /// <summary>Anchors the rect to a fraction of its parent, with no offsets.</summary>
        public static RectTransform Anchor(RectTransform rect, float xMin, float yMin, float xMax, float yMax)
        {
            rect.anchorMin = new Vector2(xMin, yMin);
            rect.anchorMax = new Vector2(xMax, yMax);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return rect;
        }

        /// <summary>A fixed-size rect pinned to one point of its parent (pivot and anchor together), offset in pixels.</summary>
        public static RectTransform Pin(RectTransform rect, Vector2 anchor, Vector2 size, Vector2 offset = default)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = anchor;
            rect.sizeDelta = size;
            rect.anchoredPosition = offset;
            return rect;
        }

        //---------- Shapes

        public static Image Fill(RectTransform rect, Color color)
        {
            var image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            return image;
        }

        /// <summary>A filled shape, or a frame of border width, on rect, with the theme's corners.</summary>
        public static UiShape Shape(RectTransform rect, Color color, float corner = 14f, float border = 0f, bool raycast = false)
        {
            var shape = rect.gameObject.AddComponent<UiShape>();
            shape.color = color;
            shape.Style = Theme.CornerStyle;
            shape.Corners = Theme.Corners;
            shape.Corner = corner;
            shape.Border = border;
            shape.raycastTarget = raycast;
            return shape;
        }

        /// <summary>A panel: a fill with a thin frame, filling rect.</summary>
        public static RectTransform PanelBox(RectTransform rect, Color? fill = null, Color? frame = null, float? corner = null)
        {
            Color panel = Theme.Panel;
            float size = corner ?? Theme.PanelCorner;
            Shape(rect, fill ?? new Color(panel.r, panel.g, panel.b, 0.9f), size, raycast: true);
            Shape(Anchor(Rect("Frame", rect), 0f, 0f, 1f, 1f), frame ?? Theme.Line, size, Theme.FrameWidth);
            return rect;
        }

        /// <summary>A vertical gradient texture, for backdrops and scrims.</summary>
        public static Texture2D Gradient(Color top, Color bottom, int height = 64)
        {
            var texture = new Texture2D(1, height, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
            };
            for (int y = 0; y < height; y++)
            {
                texture.SetPixel(0, y, Color.Lerp(bottom, top, y / (height - 1f)));
            }

            texture.Apply();
            return texture;
        }

        /// <summary>A rect filled with a vertical gradient.</summary>
        public static RawImage GradientFill(RectTransform rect, Color top, Color bottom)
        {
            var image = rect.gameObject.AddComponent<RawImage>();
            image.texture = Gradient(top, bottom);
            image.raycastTarget = false;
            return image;
        }

        //---------- Text

        /// <summary>Text in the body face, or in font.</summary>
        public static TextMeshProUGUI Text(Transform parent, string text, float size = 26f, Color? color = null,
            FontStyles style = FontStyles.Normal, TextAlignmentOptions align = TextAlignmentOptions.TopLeft,
            TMP_FontAsset font = null)
        {
            RectTransform rect = Rect("Text", parent);
            var tmp = rect.gameObject.AddComponent<TextMeshProUGUI>();
            SetFace(tmp, font != null ? font : Theme.Body);
            tmp.text = text;
            tmp.fontSize = size;
            tmp.color = color ?? Theme.Text;
            tmp.fontStyle = style;
            tmp.alignment = align;
            tmp.textWrappingMode = TextWrappingModes.Normal;
            tmp.raycastTarget = false;
            tmp.parseCtrlCharacters = false; // a backslash in a path or name stays a backslash
            return tmp;
        }

        /// <summary>The display face: titles and names.</summary>
        public static TextMeshProUGUI Title(Transform parent, string text, float size, Color? color = null,
            TextAlignmentOptions align = TextAlignmentOptions.TopLeft)
        {
            TextMeshProUGUI tmp = Text(parent, text, size, color, TitleStyle, align, Theme.Display);
            tmp.characterSpacing = 2f;
            return tmp;
        }

        /// <summary>Small spaced capitals: labels over values.</summary>
        public static TextMeshProUGUI Label(Transform parent, string text, float size = 16f, Color? color = null)
        {
            TextMeshProUGUI tmp = Text(parent, text, size, color ?? Theme.Muted, FontStyles.UpperCase, font: Theme.BodyBold);
            tmp.characterSpacing = 10f;
            return tmp;
        }

        private static FontStyles TitleStyle => Theme.UpperCaseTitles ? FontStyles.UpperCase : FontStyles.Normal;

        // Sets a text in one of the theme's faces (null: the face it has), kerned as the theme says.
        private static void SetFace(TMP_Text tmp, TMP_FontAsset face)
        {
            if (face != null)
            {
                tmp.font = face;
            }

            if (!Theme.HasKerning(tmp.font))
            {
                tmp.fontFeatures = tmp.fontFeatures.Where(feature => feature != OTL_FeatureTag.kern).ToList();
            }
        }

        //---------- Controls

        /// <summary>
        /// A button. In the theme's accent colour it is a screen's main action, lettered in the
        /// display face; any other colour (the raised colour unless given) gets a frame, and is
        /// lettered in the bold body face unless the theme sets every button in the display face.
        /// </summary>
        public static Button Button(Transform parent, string label, Action onClick, float height = 56f, Color? color = null,
            float fontSize = 24f)
        {
            UiTheme theme = Theme;
            Color fill = color ?? theme.Raised;
            bool primary = fill == theme.Accent;
            float corner = theme.ButtonCorner;
            RectTransform rect = Rect("Button: " + label, parent);
            UiShape shape = Shape(rect, fill, corner, raycast: true);
            UiShape frame = primary ? null : Shape(Anchor(Rect("Frame", rect), 0f, 0f, 1f, 1f), theme.Line, corner, theme.FrameWidth);

            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = shape;
            button.onClick.AddListener(() => onClick());
            UiSound.On(button);
            ButtonLook.On(button, shape, frame, corner);
            AddMotion(button);
            var layout = rect.gameObject.AddComponent<LayoutElement>();
            layout.preferredHeight = height;
            layout.minHeight = height;

            bool display = primary || theme.DisplayButtons;
            TextMeshProUGUI text = display
                ? Text(rect, label, fontSize + 4f, primary ? theme.OnAccent : theme.Text, TitleStyle, TextAlignmentOptions.Center, theme.Display)
                : Text(rect, label, fontSize, theme.Text, FontStyles.Normal, TextAlignmentOptions.Center, theme.BodyBold);
            text.characterSpacing = display ? 3f : 1f;
            Anchor(text.rectTransform, 0f, 0f, 1f, 1f);
            text.margin = new Vector4(14f, 4f, 14f, 4f);
            return button;
        }

        /// <summary>
        /// A labelled slider from 0 to 100%: a groove filled in the dim accent and an accent handle,
        /// with the amount at the right. Calls changed (0–1) as it moves. amount is what to show for
        /// a value, for a slider that stands for something other than a percentage, and amountWidth
        /// how wide its column is, for amounts longer than "100%".
        /// </summary>
        public static Slider Slider(Transform parent, string label, float value, Action<float> changed,
            Func<float, string> amount = null, float amountWidth = 66f)
        {
            amount ??= Percent;
            UiTheme theme = Theme;
            float corner = theme.SmallCorner;
            RectTransform row = Rect("Slider: " + label, parent);
            var layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 18f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = layout.childForceExpandHeight = false;
            var rowSize = row.gameObject.AddComponent<LayoutElement>();
            rowSize.preferredHeight = rowSize.minHeight = 44f;
            rowSize.flexibleHeight = 0f;

            TextMeshProUGUI name = Text(row, label, 24f, theme.Text, FontStyles.Normal, TextAlignmentOptions.MidlineLeft, theme.BodyBold);
            name.textWrappingMode = TextWrappingModes.NoWrap;
            name.gameObject.AddComponent<LayoutElement>().preferredWidth = 130f;

            RectTransform track = Rect("Track", row);
            var trackSize = track.gameObject.AddComponent<LayoutElement>();
            trackSize.flexibleWidth = 1f;
            trackSize.minWidth = 120f;
            trackSize.preferredHeight = 44f;
            Fill(track, Color.clear); // the whole track takes the pointer
            RectTransform groove = Anchor(Rect("Groove", track), 0f, 0.5f, 1f, 0.5f);
            groove.offsetMin = new Vector2(0f, -5f);
            groove.offsetMax = new Vector2(0f, 5f);
            Shape(groove, theme.Background, corner);
            Shape(Anchor(Rect("Frame", groove), 0f, 0f, 1f, 1f), theme.Line, corner, 1.5f);
            // The fill runs from the groove's start to the handle's middle (the slider sets only its anchors).
            RectTransform fillArea = Anchor(Rect("Fill area", track), 0f, 0.5f, 1f, 0.5f);
            fillArea.offsetMin = new Vector2(10f, -5f);
            fillArea.offsetMax = new Vector2(-10f, 5f);
            RectTransform fill = Rect("Fill", fillArea);
            fill.offsetMin = new Vector2(-10f, 0f);
            fill.offsetMax = Vector2.zero;
            Shape(fill, theme.AccentDim, corner);
            RectTransform handleArea = Anchor(Rect("Handle area", track), 0f, 0f, 1f, 1f);
            handleArea.offsetMin = new Vector2(10f, 0f);
            handleArea.offsetMax = new Vector2(-10f, 0f);
            RectTransform handle = Rect("Handle", handleArea);
            handle.sizeDelta = new Vector2(20f, -12f);
            UiShape knob = Shape(handle, theme.Accent, corner + 1f);

            var slider = track.gameObject.AddComponent<Slider>();
            slider.fillRect = fill;
            slider.handleRect = handle;
            slider.targetGraphic = knob;
            slider.direction = UnityEngine.UI.Slider.Direction.LeftToRight;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.SetValueWithoutNotify(Mathf.Clamp01(value));
            ColorBlock colors = slider.colors;
            colors.normalColor = new Color(0.86f, 0.86f, 0.86f, 1f);
            colors.highlightedColor = colors.selectedColor = Color.white;
            colors.pressedColor = new Color(0.75f, 0.75f, 0.75f, 1f);
            slider.colors = colors;

            TextMeshProUGUI shown = Text(row, amount(slider.value), 22f, theme.Muted, FontStyles.Normal, TextAlignmentOptions.MidlineRight);
            shown.gameObject.AddComponent<LayoutElement>().preferredWidth = amountWidth;
            slider.onValueChanged.AddListener(v =>
            {
                shown.text = amount(v);
                changed(v);
            });
            UiSound.On(slider);
            AddMotion(slider);
            return slider;
        }

        private static string Percent(float value) => Mathf.RoundToInt(Mathf.Clamp01(value) * 100f) + "%";

        /// <summary>
        /// A labelled toggle: the label, and at the right a box that holds an accent mark while it
        /// is on. The whole row takes the pointer. Calls changed as it flips.
        /// </summary>
        public static Toggle Toggle(Transform parent, string label, bool value, Action<bool> changed)
        {
            UiTheme theme = Theme;
            float corner = theme.SmallCorner;
            RectTransform row = Rect("Toggle: " + label, parent);
            Fill(row, Color.clear);
            var layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 18f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = layout.childForceExpandHeight = false;
            var rowSize = row.gameObject.AddComponent<LayoutElement>();
            rowSize.preferredHeight = rowSize.minHeight = 44f;
            rowSize.flexibleHeight = 0f;

            TextMeshProUGUI name = Text(row, label, 24f, theme.Text, FontStyles.Normal, TextAlignmentOptions.MidlineLeft, theme.BodyBold);
            name.textWrappingMode = TextWrappingModes.NoWrap;
            name.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;

            RectTransform box = Rect("Box", row);
            var boxSize = box.gameObject.AddComponent<LayoutElement>();
            boxSize.preferredWidth = boxSize.minWidth = 36f;
            boxSize.preferredHeight = boxSize.minHeight = 36f;
            UiShape back = Shape(box, theme.Background, corner);
            UiShape frame = Shape(Anchor(Rect("Frame", box), 0f, 0f, 1f, 1f), theme.Line, corner, theme.FrameWidth);
            RectTransform mark = Anchor(Rect("Mark", box), 0f, 0f, 1f, 1f);
            mark.offsetMin = new Vector2(8f, 8f);
            mark.offsetMax = new Vector2(-8f, -8f);
            UiShape on = Shape(mark, theme.Accent, corner);

            var toggle = row.gameObject.AddComponent<Toggle>();
            toggle.targetGraphic = back;
            toggle.graphic = on;
            toggle.SetIsOnWithoutNotify(value);
            toggle.onValueChanged.AddListener(isOn => changed(isOn));
            UiSound.On(toggle);
            ButtonLook.On(toggle, back, frame, corner);
            AddMotion(toggle);
            return toggle;
        }

        /// <summary>
        /// A labelled dropdown: the label, then a button showing the picked option, whose list opens
        /// under it. Calls changed with the index of the option picked.
        /// </summary>
        public static TMP_Dropdown Dropdown(Transform parent, string label, IList<string> options, int value, Action<int> changed)
        {
            UiTheme theme = Theme;
            float corner = theme.ButtonCorner;
            RectTransform row = Rect("Dropdown: " + label, parent);
            var layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 18f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = layout.childForceExpandHeight = false;
            var rowSize = row.gameObject.AddComponent<LayoutElement>();
            rowSize.preferredHeight = rowSize.minHeight = DropdownHeight;
            rowSize.flexibleHeight = 0f;

            TextMeshProUGUI name = Text(row, label, 24f, theme.Text, FontStyles.Normal, TextAlignmentOptions.MidlineLeft, theme.BodyBold);
            name.textWrappingMode = TextWrappingModes.NoWrap;
            name.gameObject.AddComponent<LayoutElement>().preferredWidth = 130f;

            // TextMeshPro's own dropdown, which brings the list, its scrolling and its navigation; its
            // sprites are replaced by the theme's shapes and colours.
            GameObject root = TMP_DefaultControls.CreateDropdown(new TMP_DefaultControls.Resources());
            root.name = "Button";
            root.transform.SetParent(row, false);
            var size = root.AddComponent<LayoutElement>();
            size.flexibleWidth = 1f;
            size.minWidth = 120f;
            size.preferredHeight = size.minHeight = DropdownHeight;
            var dropdown = root.GetComponent<TMP_Dropdown>();
            root.GetComponent<Image>().color = Color.clear; // still takes the pointer

            RectTransform fillRect = Anchor(Rect("Fill", root.transform), 0f, 0f, 1f, 1f);
            fillRect.SetAsFirstSibling();
            UiShape fill = Shape(fillRect, theme.Raised, corner);
            RectTransform frameRect = Anchor(Rect("Frame", root.transform), 0f, 0f, 1f, 1f);
            frameRect.SetSiblingIndex(1);
            UiShape frame = Shape(frameRect, theme.Line, corner, theme.FrameWidth);
            dropdown.targetGraphic = fill;

            TMP_Text caption = dropdown.captionText;
            SetFace(caption, theme.BodyBold);
            caption.fontSize = 24f;
            caption.color = theme.Text;
            caption.alignment = TextAlignmentOptions.MidlineLeft;
            caption.textWrappingMode = TextWrappingModes.NoWrap;
            caption.overflowMode = TextOverflowModes.Ellipsis;
            caption.rectTransform.offsetMin = new Vector2(18f, 4f);
            caption.rectTransform.offsetMax = new Vector2(-48f, -4f);

            // The arrow: a square whose lower corners are cut to a point.
            var arrow = (RectTransform)root.transform.Find("Arrow");
            arrow.GetComponent<Image>().enabled = false;
            arrow.sizeDelta = new Vector2(16f, 16f);
            arrow.anchoredPosition = new Vector2(-26f, 0f);
            UiShape point = Shape(Anchor(Rect("Point", arrow), 0f, 0f, 1f, 1f), theme.Accent, 8f);
            point.Style = UiCornerStyle.Cut;
            point.Corners = UiCorners.BottomLeft | UiCorners.BottomRight;

            // The list: as tall as its options, up to a few, then it scrolls.
            RectTransform template = dropdown.template;
            template.anchoredPosition = new Vector2(0f, -4f);
            template.sizeDelta = new Vector2(0f, 6.5f * DropdownItemHeight);
            template.GetComponent<Image>().color = theme.Panel;
            UiShape listFrame = Shape(Anchor(Rect("Frame", template), 0f, 0f, 1f, 1f), theme.Line, 0f, theme.FrameWidth);
            listFrame.Corners = UiCorners.None;
            var scroll = template.GetComponent<ScrollRect>();
            scroll.scrollSensitivity = 40f;
            scroll.content.sizeDelta = new Vector2(0f, DropdownItemHeight + 8f);
            var bar = (RectTransform)scroll.verticalScrollbar.transform;
            bar.sizeDelta = new Vector2(14f, 0f);
            bar.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.06f);
            scroll.verticalScrollbar.targetGraphic.color = theme.Muted;

            // One option, which the dropdown copies for each. Its background shows only while the
            // pointer or the selection is on it; the picked one has the accent mark.
            TMP_Text itemText = dropdown.itemText;
            var item = itemText.GetComponentInParent<Toggle>(true);
            ((RectTransform)item.transform).sizeDelta = new Vector2(0f, DropdownItemHeight);
            item.targetGraphic.color = theme.Raised;
            ColorBlock colors = item.colors;
            colors.normalColor = new Color(0.7f, 0.7f, 0.7f, 0f);
            colors.highlightedColor = colors.selectedColor = Color.white;
            colors.pressedColor = new Color(0.75f, 0.75f, 0.75f, 1f);
            colors.fadeDuration = 0.08f;
            item.colors = colors;
            var check = (RectTransform)item.graphic.transform;
            item.graphic.enabled = false;
            check.sizeDelta = new Vector2(12f, 12f);
            check.anchoredPosition = new Vector2(22f, 0f);
            item.graphic = Shape(Anchor(Rect("Mark", check), 0f, 0f, 1f, 1f), theme.Accent, 3f);
            SetFace(itemText, theme.Body);
            itemText.fontSize = 24f;
            itemText.color = theme.Text;
            itemText.alignment = TextAlignmentOptions.MidlineLeft;
            itemText.textWrappingMode = TextWrappingModes.NoWrap;
            itemText.overflowMode = TextOverflowModes.Ellipsis;
            itemText.rectTransform.offsetMin = new Vector2(44f, 0f);
            itemText.rectTransform.offsetMax = new Vector2(-12f, 0f);
            UiSound.On(item);

            dropdown.ClearOptions();
            dropdown.AddOptions(new List<string>(options));
            dropdown.SetValueWithoutNotify(value);
            dropdown.onValueChanged.AddListener(index => changed(index));
            UiSound.On(dropdown);
            ButtonLook.On(dropdown, fill, frame, corner);
            AddMotion(dropdown);
            return dropdown;
        }

        /// <summary>A one-line text field. Calls changed when the player leaves it.</summary>
        public static TMP_InputField InputField(Transform parent, string text, Action<string> changed,
            TMP_InputField.ContentType type = TMP_InputField.ContentType.Standard, string placeholder = "")
        {
            UiTheme theme = Theme;
            GameObject input = TMP_DefaultControls.CreateInputField(new TMP_DefaultControls.Resources());
            input.transform.SetParent(parent, false);
            input.AddComponent<LayoutElement>().preferredHeight = 56f;
            var field = input.GetComponent<TMP_InputField>();
            var box = input.GetComponent<Image>();
            box.sprite = null;
            box.color = theme.Background;
            Shape(Anchor(Rect("Frame", input.transform), 0f, 0f, 1f, 1f), theme.Line, 0f, theme.FrameWidth);
            TMP_FontAsset body = theme.Body;
            foreach (TMP_Text label in input.GetComponentsInChildren<TMP_Text>(true))
            {
                if (body != null)
                {
                    label.font = body;
                }

                label.color = label == field.placeholder ? theme.Muted : theme.Text;
            }

            Color accent = theme.Accent;
            field.caretColor = accent;
            field.selectionColor = new Color(accent.r, accent.g, accent.b, 0.35f);
            field.contentType = type;
            field.pointSize = 26f;
            field.text = text;
            ((TextMeshProUGUI)field.placeholder).text = placeholder;
            field.onEndEdit.AddListener(value => changed(value));
            return field;
        }

        // The theme's swell and squash, on a control the kit built.
        private static void AddMotion(Selectable control)
        {
            if (Theme.HasMotion)
            {
                Squishy.On(control.gameObject, Theme);
            }
        }

        //---------- Layout

        /// <summary>A vertically scrolling column filling area; returns the content to add children to.</summary>
        public static RectTransform ScrollColumn(RectTransform area, float spacing = 14f, int padding = 24)
        {
            // The mouse wheel goes to whatever the pointer is over and up from there, so the area
            // itself must take the pointer: a panel behind it would swallow the wheel.
            if (area.GetComponent<Graphic>() == null)
            {
                Fill(area, Color.clear);
            }

            var scroll = area.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.scrollSensitivity = 40f;

            // A visible scrollbar on the right, so it is obvious when a panel holds more than fits.
            RectTransform viewport = Anchor(Rect("Viewport", area), 0f, 0f, 1f, 1f);
            viewport.offsetMax = new Vector2(-18f, 0f);
            RectTransform bar = Anchor(Rect("Scrollbar", area), 1f, 0f, 1f, 1f);
            bar.offsetMin = new Vector2(-14f, 4f);
            bar.offsetMax = new Vector2(-2f, -4f);
            Fill(bar, new Color(1f, 1f, 1f, 0.06f));
            RectTransform handleArea = Anchor(Rect("Sliding Area", bar), 0f, 0f, 1f, 1f);
            RectTransform handle = Anchor(Rect("Handle", handleArea), 0f, 0f, 1f, 1f);
            Image handleImage = Fill(handle, Theme.Muted);
            var scrollbar = bar.gameObject.AddComponent<Scrollbar>();
            scrollbar.direction = Scrollbar.Direction.BottomToTop;
            scrollbar.handleRect = handle;
            scrollbar.targetGraphic = handleImage;
            scroll.verticalScrollbar = scrollbar;
            scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
            viewport.gameObject.AddComponent<RectMask2D>();
            RectTransform content = Rect("Content", viewport);
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.offsetMin = content.offsetMax = Vector2.zero;

            var layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = spacing;
            layout.padding = new RectOffset(padding, padding, padding, padding);
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scroll.viewport = viewport;
            scroll.content = content;
            return content;
        }

        /// <summary>A row of equal-width buttons or other children inside a vertical column.</summary>
        public static RectTransform Row(Transform parent, float height, float spacing = 10f)
        {
            RectTransform rect = Rect("Row", parent);
            var layout = rect.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = spacing;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;
            var size = rect.gameObject.AddComponent<LayoutElement>();
            size.preferredHeight = height;
            size.flexibleHeight = 0f; // a row never grows into a column's spare space
            return rect;
        }

        public static void Spacer(Transform parent, float height) =>
            Rect("Spacer", parent).gameObject.AddComponent<LayoutElement>().preferredHeight = height;
    }
}
