using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Aurumworks
{
    /// <summary>
    /// The menus open over a game's screen, one on top of another: the session menu, Settings over
    /// it, a dialog over that. Each dims and blocks what is under it, and opens with its main action
    /// selected. The UI Menu action (Escape, the gamepad's Start) and UI Cancel (Escape, the gamepad's
    /// East) close the top one; with none open they are the game's, through MenuPressed and
    /// BackPressed. Nothing pauses, but gameplay gives way while any is open (MenuFocus). A game has
    /// one, on the rect its menus are drawn in (MenuStack.On).
    /// </summary>
    public class MenuStack : MonoBehaviour
    {
        private struct Menu
        {
            public RectTransform Panel;
            public Action Closed;
        }

        private static readonly Color _scrim = new(0f, 0f, 0f, 0.8f);

        private readonly List<Menu> _menus = new();
        private RectTransform _screen;
        private InputAction _menuAction, _cancelAction;
        private InputAction _madeMenuAction, _madeCancelAction;

        /// <summary>Menu was pressed with no menu open: the game opens its session menu, if a session is under way.</summary>
        public event Action MenuPressed;

        /// <summary>Cancel was pressed with no menu open: the game goes back a step, if its screen has one.</summary>
        public event Action BackPressed;

        /// <summary>How wide a dialog is, as a share of the screen's width: 0.4 unless the game sets it.</summary>
        public float DialogWidth { get; set; } = 0.4f;

        /// <summary>How many menus are open.</summary>
        public int Count => _menus.Count;

        /// <summary>The menu on top, or null.</summary>
        public RectTransform Top => _menus.Count > 0 ? _menus[_menus.Count - 1].Panel : null;

        /// <summary>
        /// The game's screen under the menus, or null. Its main action takes the selection back when
        /// the last menu closes, and navigation can't reach it while one is open.
        /// </summary>
        public RectTransform Screen
        {
            get => _screen;
            set
            {
                _screen = value;
                SetReachable(value, _menus.Count == 0);
            }
        }

        /// <summary>Gives rect a stack; its menus are drawn in rect, over rect's other children.</summary>
        public static MenuStack On(RectTransform rect) => rect.gameObject.AddComponent<MenuStack>();

        //----------

        private void OnEnable()
        {
            _menuAction = FindAction("UI/Menu") ?? (_madeMenuAction = MakeAction("Menu", "<Keyboard>/escape", "<Gamepad>/start"));
            _cancelAction = FindAction("UI/Cancel") ?? (_madeCancelAction = MakeAction("Cancel", "*/{Cancel}"));
            _menuAction.Enable();
            _cancelAction.Enable();
        }

        private void OnDisable()
        {
            _madeMenuAction?.Dispose();
            _madeCancelAction?.Dispose();
            _menuAction = _cancelAction = _madeMenuAction = _madeCancelAction = null;
        }

        private void OnDestroy() => MenuFocus.SetOpen(this, false);

        // Read here, not in the actions' callbacks: Escape is bound to both actions, which fire in no
        // fixed order, and one press must do one thing.
        private void Update()
        {
            bool menu = _menuAction.WasPressedThisFrame();
            if (!menu && !_cancelAction.WasPressedThisFrame())
            {
                return;
            }

            if (_menus.Count == 0)
            {
                (menu ? MenuPressed : BackPressed)?.Invoke();
            }
            else if (!HasOpenDropdown(Top))
            {
                Close();
            }
        }

        // The project's own actions (Project Settings > Input System Package), so its bindings apply.
        private static InputAction FindAction(string path) => InputSystem.actions != null ? InputSystem.actions.FindAction(path) : null;

        // A project without the action gets the bindings every Aurumworks game uses.
        private static InputAction MakeAction(string name, params string[] bindings)
        {
            var action = new InputAction(name, InputActionType.Button);
            foreach (string binding in bindings)
            {
                action.AddBinding(binding);
            }

            return action;
        }

        // An open dropdown list closes itself on Cancel, so the same press shouldn't close its menu.
        private static bool HasOpenDropdown(RectTransform menu)
        {
            foreach (var dropdown in menu.GetComponentsInChildren<TMP_Dropdown>())
            {
                if (dropdown.IsExpanded)
                {
                    return true;
                }
            }

            return false;
        }

        //----------

        /// <summary>
        /// Opens an empty menu over everything: a rect filling the stack's, dimming and blocking what
        /// is under it. The caller builds in it, then calls SelectMainAction. closed is called when
        /// it closes, however it closes.
        /// </summary>
        public RectTransform Open(string name, Action closed = null)
        {
            RectTransform panel = UiKit.Anchor(UiKit.Rect(name, transform), 0f, 0f, 1f, 1f);
            UiKit.Fill(panel, _scrim);
            SetReachable(_menus.Count > 0 ? Top : Screen, false);
            _menus.Add(new Menu { Panel = panel, Closed = closed });
            MenuFocus.SetOpen(this, true);
            return panel;
        }

        /// <summary>
        /// Opens a dialog: a centred box DialogWidth wide and as tall as what build puts in its
        /// column, with its main action selected.
        /// </summary>
        public RectTransform OpenDialog(string name, Action<RectTransform> build, Action closed = null)
        {
            RectTransform panel = Open(name, closed);
            UiTheme theme = UiKit.Theme;
            float corner = theme.PanelCorner * 1.25f;
            RectTransform box = UiKit.Rect("Box", panel);
            float half = Mathf.Clamp(DialogWidth, 0.1f, 1f) / 2f;
            box.anchorMin = new Vector2(0.5f - half, 0.5f);
            box.anchorMax = new Vector2(0.5f + half, 0.5f);
            box.pivot = new Vector2(0.5f, 0.5f);
            box.offsetMin = box.offsetMax = Vector2.zero;
            UiKit.Shape(box, theme.Panel, corner, raycast: true);
            UiShape frame = UiKit.Shape(UiKit.Anchor(UiKit.Rect("Frame", box), 0f, 0f, 1f, 1f), theme.AccentDim, corner, theme.FrameWidth);
            frame.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            var layout = box.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 16f;
            layout.padding = new RectOffset(40, 40, 36, 40);
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            box.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            build(box);
            SelectMainAction(panel);
            return panel;
        }

        /// <summary>
        /// Asks before something that can't be undone. The safe answer is the main action, so Submit
        /// never does the damage; confirmed is called, after the dialog has closed, only for the other.
        /// </summary>
        public RectTransform Confirm(string name, string title, string body, string warning, string keepLabel, string confirmLabel,
            Action confirmed)
        {
            return OpenDialog(name, column =>
            {
                UiTheme theme = UiKit.Theme;
                UiKit.Title(column, title, 52f, theme.Accent, TextAlignmentOptions.Center);
                if (!string.IsNullOrEmpty(body))
                {
                    UiKit.Text(column, body, 22f, align: TextAlignmentOptions.Center);
                }

                if (!string.IsNullOrEmpty(warning))
                {
                    UiKit.Text(column, warning, 22f, theme.Muted, align: TextAlignmentOptions.Center);
                }

                UiKit.Spacer(column, 6f);
                RectTransform row = UiKit.Row(column, 60f, 16f);
                UiKit.Button(row, keepLabel, Close, 60f, theme.Accent);
                UiKit.Button(row, confirmLabel, () =>
                {
                    Close();
                    confirmed();
                }, 60f, theme.Danger);
            });
        }

        /// <summary>Closes the top menu, and hands the selection to the one under it, or to the game's screen.</summary>
        public void Close()
        {
            if (_menus.Count == 0)
            {
                return;
            }

            Menu top = _menus[_menus.Count - 1];
            _menus.RemoveAt(_menus.Count - 1);
            MenuFocus.SetOpen(this, _menus.Count > 0);
            if (top.Panel != null)
            {
                Destroy(top.Panel.gameObject);
            }

            top.Closed?.Invoke();
            RectTransform under = _menus.Count > 0 ? Top : Screen;
            SetReachable(under, true);
            SelectMainAction(under);
        }

        /// <summary>Closes every menu, from the top down.</summary>
        public void CloseAll()
        {
            while (_menus.Count > 0)
            {
                Close();
            }
        }

        // The dimming stops the pointer, but keyboard and gamepad navigation goes to the nearest
        // control in a direction, whatever is drawn over it. So what is under the top menu is
        // switched off as a group, which navigation skips; ButtonLook leaves its look alone.
        private static void SetReachable(RectTransform under, bool reachable)
        {
            if (under == null)
            {
                return;
            }

            var group = under.GetComponent<CanvasGroup>();
            if (group == null)
            {
                if (reachable)
                {
                    return;
                }

                group = under.gameObject.AddComponent<CanvasGroup>();
            }

            group.interactable = reachable;
        }

        /// <summary>
        /// Selects a screen's main action (its first usable button in the accent colour, else its
        /// first usable button), so Submit presses it and navigation starts from it.
        /// </summary>
        public static void SelectMainAction(RectTransform screen)
        {
            if (screen == null || EventSystem.current == null)
            {
                return;
            }

            Color accent = UiKit.Theme.Accent;
            Button first = null;
            foreach (var button in screen.GetComponentsInChildren<Button>())
            {
                if (!button.IsInteractable())
                {
                    continue;
                }

                if (button.targetGraphic != null && button.targetGraphic.color == accent)
                {
                    first = button;
                    break;
                }

                if (first == null)
                {
                    first = button;
                }
            }

            EventSystem.current.SetSelectedGameObject(first != null ? first.gameObject : null);
        }
    }
}
