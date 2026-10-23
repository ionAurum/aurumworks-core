using System.Collections.Generic;
using UnityEngine;

namespace Aurumworks
{
    /// <summary>
    /// Static: which menus are open over the game. While any is, gameplay gives way: whatever steers
    /// or turns a camera stops (the sticks and arrow keys navigate the menu instead) and the cursor
    /// is freed. Menus report themselves open and closed, and closed when destroyed; a MenuStack
    /// reports for the menus it holds.
    /// </summary>
    public static class MenuFocus
    {
        private static readonly HashSet<Object> _openMenus = new();

        /// <summary>True while any menu is open.</summary>
        public static bool IsAnyOpen => _openMenus.Count > 0;

        // Statics survive play mode when domain reload is disabled; reset them.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => _openMenus.Clear();

        /// <summary>True while a menu other than this one is open: one over it, or one it should wait for.</summary>
        public static bool IsOpenBesides(Object menu)
        {
            foreach (var openMenu in _openMenus)
            {
                if (openMenu != menu)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>Reports a menu opening or closing. Reporting the same state twice is harmless.</summary>
        public static void SetOpen(Object menu, bool open)
        {
            if (open)
            {
                _openMenus.Add(menu);
            }
            else
            {
                _openMenus.Remove(menu);
            }
        }
    }
}
