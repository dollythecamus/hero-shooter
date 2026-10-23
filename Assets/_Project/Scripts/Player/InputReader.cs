using UnityEngine;
using UnityEngine.InputSystem;

namespace HeroShooter.Player
{
    /// <summary>
    /// The one place that reads hardware. Gameplay code asks this class instead of touching
    /// <see cref="Keyboard"/>/<see cref="Mouse"/> directly, so rebinding or a controller can be
    /// added later without editing a single gameplay script.
    /// Values are read at call time — no cached frame state, no script-execution-order dependency.
    /// </summary>
    public static class InputReader
    {
        static Keyboard Kb => Keyboard.current;
        static Mouse Ms => Mouse.current;
        static Gamepad Pad => Gamepad.current;

        /// <summary>WASD or left stick, unclamped (length can exceed 1 for diagonals — normalize at use).</summary>
        public static Vector2 Move
        {
            get
            {
                Vector2 value = Vector2.zero;

                if (Kb != null)
                {
                    if (Kb.wKey.isPressed) value.y += 1f;
                    if (Kb.sKey.isPressed) value.y -= 1f;
                    if (Kb.dKey.isPressed) value.x += 1f;
                    if (Kb.aKey.isPressed) value.x -= 1f;
                }

                if (Pad != null)
                {
                    value += Pad.leftStick.ReadValue();
                }

                return value;
            }
        }

        /// <summary>Mouse delta or right stick. Raw pixels; sensitivity lives in the camera rig.</summary>
        public static Vector2 Look
        {
            get
            {
                Vector2 value = Ms != null ? Ms.delta.ReadValue() : Vector2.zero;

                if (Pad != null)
                {
                    value += Pad.rightStick.ReadValue() * 12f;
                }

                return value;
            }
        }

        public static bool FireHeld => Ms != null && Ms.leftButton.isPressed;
        public static bool FirePressed => Ms != null && Ms.leftButton.wasPressedThisFrame;

        public static bool SprintHeld =>
            (Kb != null && Kb.leftShiftKey.isPressed) || (Pad != null && Pad.leftStickButton.isPressed);

        public static bool JumpPressed =>
            (Kb != null && Kb.spaceKey.wasPressedThisFrame) || (Pad != null && Pad.buttonSouth.wasPressedThisFrame);

        public static bool Ability1Pressed =>
            (Kb != null && Kb.qKey.wasPressedThisFrame) || (Ms != null && Ms.rightButton.wasPressedThisFrame);

        public static bool Ability2Pressed =>
            (Kb != null && Kb.eKey.wasPressedThisFrame) || (Ms != null && Ms.middleButton.wasPressedThisFrame);

        /// <summary>Hold Tab to read the frag board.</summary>
        public static bool FragsHeld => Kb != null && Kb.tabKey.isPressed;

        public static bool PausePressed => Kb != null && Kb.escapeKey.wasPressedThisFrame;

        public static void SetCursorLocked(bool locked)
        {
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }
    }
}
