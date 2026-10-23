using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Aurumworks
{
    /// <summary>
    /// Static: what a self-test asks of the screen in front of it. Where a tap or the mouse wheel
    /// would land, which controls a player could use, what keyboard and gamepad navigation can
    /// reach, and a press on a control where a player would press it. The pointer checks need a
    /// window: a graphic takes taps once it has been drawn, so in batch mode they pass unasked.
    /// </summary>
    public static class UiProbe
    {
        /// <summary>
        /// What a pointer at the middle of target would hit first, when that isn't target or
        /// something in it; null when the tap lands where it should.
        /// </summary>
        public static string PointerHit(RectTransform target)
        {
            if (EventSystem.current == null)
            {
                return null;
            }

            Vector2 at = ScreenMiddle(target);
            if (at.x < 0f || at.y < 0f || at.x > Screen.width || at.y > Screen.height)
            {
                return "nothing: it is off the screen at " + at;
            }

            List<RaycastResult> hits = Raycast(at);
            if (hits.Count == 0)
            {
                return "nothing at " + at;
            }

            Transform first = hits[0].gameObject.transform;
            if (first.IsChildOf(target))
            {
                return null;
            }

            var path = new List<string>();
            for (Transform parent = first; parent != null && path.Count < 5; parent = parent.parent)
            {
                path.Add(parent.name);
            }

            return string.Join(" < ", path);
        }

        /// <summary>
        /// Logs an error for every scroll view under root that the mouse wheel wouldn't reach at its
        /// middle. The wheel goes to whatever is under the pointer and up from there, so a panel
        /// drawn in front of a scroll view swallows it. where names the screen in the error.
        /// </summary>
        public static void CheckWheel(RectTransform root, string where)
        {
            if (Application.isBatchMode || EventSystem.current == null)
            {
                return;
            }

            foreach (var scroll in root.GetComponentsInChildren<ScrollRect>())
            {
                RectTransform view = scroll.viewport != null ? scroll.viewport : (RectTransform)scroll.transform;
                List<RaycastResult> hits = Raycast(ScreenMiddle(view));
                Transform over = hits.Count > 0 ? hits[0].gameObject.transform : null;

                // Something else is in front of it: an open menu, a pop-up.
                if (over != null && !over.IsChildOf(scroll.transform) && !scroll.transform.IsChildOf(over))
                {
                    continue;
                }

                GameObject taker = over != null ? ExecuteEvents.GetEventHandler<IScrollHandler>(over.gameObject) : null;
                if (taker != scroll.gameObject)
                {
                    string goesTo = taker != null ? taker.name : over != null ? "nothing, over " + over.name : "nothing";
                    Debug.LogError($"Wheel check: {where}: the scroll view {scroll.transform.parent.name}/{scroll.name} " +
                                   $"doesn't get the mouse wheel; it goes to {goesTo}");
                }
            }
        }

        /// <summary>The first active control of a kind under root with this object name ("Button: Settings"), or null.</summary>
        public static T Find<T>(Transform root, string name) where T : Component
        {
            foreach (var candidate in root.GetComponentsInChildren<T>())
            {
                if (candidate.name == name)
                {
                    return candidate;
                }
            }

            return null;
        }

        /// <summary>Every text showing under root, in the order it was built, for the log.</summary>
        public static string Texts(Transform root)
        {
            var texts = new List<string>();
            foreach (var text in root.GetComponentsInChildren<TMP_Text>())
            {
                if (!string.IsNullOrWhiteSpace(text.text))
                {
                    texts.Add(text.text);
                }
            }

            return string.Join(" | ", texts);
        }

        /// <summary>True when a player could use the control now: it is switched on, and a tap at its middle lands on it.</summary>
        public static bool IsUsable(Selectable control) =>
            control != null && control.isActiveAndEnabled && control.IsInteractable() &&
            (Application.isBatchMode || PointerHit((RectTransform)control.transform) == null);

        /// <summary>True when any control under root is switched on, whatever is drawn over it.</summary>
        public static bool HasInteractable(Transform root)
        {
            foreach (var control in root.GetComponentsInChildren<Selectable>())
            {
                if (control.IsInteractable())
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Clicks a control where a player would, through the event system. Logs an error and
        /// returns false when it can't be used (see IsUsable).
        /// </summary>
        public static bool Press(Selectable control, string name = null)
        {
            if (!IsUsable(control))
            {
                string hit = control != null && !Application.isBatchMode ? PointerHit((RectTransform)control.transform) : null;
                Debug.LogError("Self-test failed: " + (control != null ? control.name : name ?? "a control") +
                               (control == null ? " isn't there" : hit != null ? ": a tap lands on " + hit : " is switched off"));
                return false;
            }

            var click = new PointerEventData(EventSystem.current)
            {
                button = PointerEventData.InputButton.Left,
                position = ScreenMiddle((RectTransform)control.transform),
            };
            ExecuteEvents.Execute(control.gameObject, click, ExecuteEvents.pointerClickHandler);
            return true;
        }

        /// <summary>
        /// Every control keyboard and gamepad navigation can get to from the selected one, itself
        /// included, without moving the selection or changing a value. Empty when nothing is selected.
        /// </summary>
        public static List<Selectable> Reachable()
        {
            var reached = new List<Selectable>();
            GameObject selected = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
            if (selected == null || !selected.TryGetComponent(out Selectable start))
            {
                return reached;
            }

            reached.Add(start);
            for (int i = 0; i < reached.Count; i++)
            {
                Selectable from = reached[i];
                AddNew(reached, from.FindSelectableOnUp());
                AddNew(reached, from.FindSelectableOnDown());
                AddNew(reached, from.FindSelectableOnLeft());
                AddNew(reached, from.FindSelectableOnRight());
            }

            return reached;
        }

        //----------

        private static void AddNew(List<Selectable> reached, Selectable next)
        {
            if (next != null && !reached.Contains(next))
            {
                reached.Add(next);
            }
        }

        // An overlay canvas draws in screen pixels; any other is seen through its camera.
        private static Vector2 ScreenMiddle(RectTransform rect)
        {
            Canvas canvas = rect.GetComponentInParent<Canvas>();
            Camera camera = canvas != null && canvas.rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? canvas.rootCanvas.worldCamera
                : null;
            return RectTransformUtility.WorldToScreenPoint(camera, rect.TransformPoint(rect.rect.center));
        }

        private static List<RaycastResult> Raycast(Vector2 at)
        {
            var hits = new List<RaycastResult>();
            EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current) { position = at }, hits);
            return hits;
        }
    }
}
