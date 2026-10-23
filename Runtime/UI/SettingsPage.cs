using System;
using TMPro;
using UnityEngine;

namespace Aurumworks
{
    /// <summary>
    /// The Settings page, the same from the main menu and the session menu: Fullscreen, a slider for
    /// each volume the game has listed (Settings.AddVolume), whatever else the game adds (a name,
    /// mouse sensitivity), and Back. A setting applies as it changes, and the page saves them all
    /// as it closes. A game opens a new page each time, so the page shows the values of that moment.
    /// </summary>
    public sealed class SettingsPage
    {
        /// <summary>Adds the game's own settings to the column, under the volumes.</summary>
        public Action<RectTransform> More { get; set; }

        /// <summary>Opens the page over whatever is showing.</summary>
        public RectTransform Open(MenuStack menus)
        {
            return menus.OpenDialog("Settings", column =>
            {
                UiTheme theme = UiKit.Theme;
                UiKit.Title(column, "Settings", 60f, theme.Accent, TextAlignmentOptions.Center);

                // Read on every open: Alt+Enter can change it behind the menu's back.
                UiKit.Toggle(column, "Fullscreen", Settings.IsFullScreen, Settings.SetFullScreen);
                if (Settings.Volumes.Count > 0)
                {
                    UiKit.Text(column, "Volume", 26f, theme.Muted);
                }

                foreach (VolumeSetting volume in Settings.Volumes)
                {
                    UiKit.Slider(column, volume.Name, volume.Value, value => volume.Value = value);
                }

                if (More != null)
                {
                    UiKit.Spacer(column, 4f);
                    More(column);
                }

                UiKit.Spacer(column, 10f);
                UiKit.Button(column, "Back", menus.Close, 60f, theme.Accent);
            }, Settings.Save);
        }
    }
}
