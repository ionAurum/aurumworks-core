using System.Collections;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace Aurumworks
{
    /// <summary>
    /// Static: key and gamepad presses for a self-test, queued as if a device had sent them, so
    /// they reach the game through its input actions and the UI's navigation as a player's would.
    /// Each is a step: the press, then the release, with frames between for the game to see both.
    /// </summary>
    public static class QueuedInput
    {
        // A press must be seen by an Update and by the UI's own update before it is released.
        private const int FramesHeld = 2;

        /// <summary>
        /// Lets input through whether or not the window has the focus, for the rest of the run. A
        /// player started by a script often opens behind other windows, where the input system
        /// would otherwise drop what is queued. The game must also run in the background.
        /// </summary>
        public static void IgnoreFocus() => InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;

        /// <summary>Presses and releases a key.</summary>
        public static IEnumerator Press(Key key)
        {
            Keyboard keyboard = Keyboard.current ?? InputSystem.AddDevice<Keyboard>();
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(key));
            yield return Hold();
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            yield return Hold();
        }

        /// <summary>Presses and releases a gamepad button, on a gamepad made for the run if none is plugged in.</summary>
        public static IEnumerator Press(GamepadButton button)
        {
            Gamepad gamepad = Gamepad.current ?? InputSystem.AddDevice<Gamepad>();
            InputSystem.QueueStateEvent(gamepad, new GamepadState(button));
            yield return Hold();
            InputSystem.QueueStateEvent(gamepad, new GamepadState());
            yield return Hold();
        }

        private static IEnumerator Hold()
        {
            for (int i = 0; i < FramesHeld; i++)
            {
                yield return null;
            }
        }
    }
}
