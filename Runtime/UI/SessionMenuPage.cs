using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Aurumworks
{
    /// <summary>
    /// The session menu, which Menu opens during a game: where the session stands, then Resume,
    /// Settings, the game's own buttons (How to play, Invite friends) and, last and in the danger
    /// colour, the way out. It is built as it opens: a game whose status changes under an open menu
    /// sets the lines again (SetStatus), and one whose buttons change closes it and opens a new one.
    /// Nothing pauses while it is open.
    /// </summary>
    public sealed class SessionMenuPage
    {
        private readonly List<(string Label, Action Pressed)> _buttons = new();
        private readonly List<TextMeshProUGUI> _statusLines = new();
        private RectTransform _column;
        private int _statusIndex; // where the status lines start among the column's children

        /// <summary>Lines under the title: the session's status.</summary>
        public IEnumerable<string> Status { get; set; }

        /// <summary>Adds what the game shows under the status (who is playing, a level picker), or null.</summary>
        public Action<RectTransform> Details { get; set; }

        /// <summary>Opens the game's Settings page over this menu.</summary>
        public Action Settings { get; set; }

        /// <summary>What the way out is called here: "Leave the game", "End the session for everyone".</summary>
        public string LeaveLabel { get; set; } = "Leave";

        /// <summary>Ends or leaves the session. The game closes the menus as it goes.</summary>
        public Action Leave { get; set; }

        /// <summary>Called when the menu closes.</summary>
        public Action Closed { get; set; }

        /// <summary>Adds one of the game's own buttons, between Settings and the way out.</summary>
        public SessionMenuPage Button(string label, Action pressed)
        {
            _buttons.Add((label, pressed));
            return this;
        }

        /// <summary>Opens the menu over the game.</summary>
        public RectTransform Open(MenuStack menus)
        {
            return menus.OpenDialog("SessionMenu", column =>
            {
                UiTheme theme = UiKit.Theme;
                UiKit.Title(column, "Menu", 60f, theme.Accent, TextAlignmentOptions.Center);
                _column = column;
                _statusIndex = column.childCount;
                _statusLines.Clear();
                ShowStatus();
                Details?.Invoke(column);
                UiKit.Spacer(column, 10f);
                UiKit.Button(column, "Resume", menus.Close, 60f, theme.Accent);
                if (Settings != null)
                {
                    UiKit.Button(column, "Settings", Settings, 60f);
                }

                foreach (var button in _buttons)
                {
                    UiKit.Button(column, button.Label, button.Pressed, 60f);
                }

                if (Leave != null)
                {
                    UiKit.Button(column, LeaveLabel, Leave, 60f, theme.Danger);
                }
            }, Closed);
        }

        /// <summary>Changes the status lines, in the open menu too. Only a line that differs is touched.</summary>
        public void SetStatus(IEnumerable<string> lines)
        {
            Status = lines;
            if (_column != null)
            {
                ShowStatus();
            }
        }

        private void ShowStatus()
        {
            int count = 0;
            if (Status != null)
            {
                foreach (string line in Status)
                {
                    if (count == _statusLines.Count)
                    {
                        TextMeshProUGUI added = UiKit.Text(_column, line, 24f, UiKit.Theme.Muted);
                        added.transform.SetSiblingIndex(_statusIndex + count);
                        _statusLines.Add(added);
                    }

                    TextMeshProUGUI text = _statusLines[count++];
                    if (text.text != line)
                    {
                        text.text = line;
                    }

                    if (!text.gameObject.activeSelf)
                    {
                        text.gameObject.SetActive(true);
                    }
                }
            }

            for (int i = count; i < _statusLines.Count; i++)
            {
                if (_statusLines[i].gameObject.activeSelf)
                {
                    _statusLines[i].gameObject.SetActive(false);
                }
            }
        }
    }
}
