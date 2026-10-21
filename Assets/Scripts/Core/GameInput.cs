using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace MiniRPG
{
    /// <summary>
    /// Lecture du clavier et de la souris, compatible avec les deux systèmes de saisie de Unity :
    /// le package Input System (par défaut sous Unity 6) et l'ancien Input Manager.
    /// Le reste du code n'utilise que cette classe.
    /// </summary>
    public static class GameInput
    {
#if ENABLE_INPUT_SYSTEM
        /// <summary>Déplacement (x = droite, y = avant). Les touches de l'Input System sont physiques :
        /// WASD en QWERTY correspond à ZQSD en AZERTY.</summary>
        public static Vector2 Move()
        {
            var k = Keyboard.current;
            if (k == null) return Vector2.zero;
            float x = (k.dKey.isPressed || k.rightArrowKey.isPressed ? 1f : 0f) - (k.aKey.isPressed || k.leftArrowKey.isPressed ? 1f : 0f);
            float y = (k.wKey.isPressed || k.upArrowKey.isPressed ? 1f : 0f) - (k.sKey.isPressed || k.downArrowKey.isPressed ? 1f : 0f);
            return new Vector2(x, y);
        }

        public static bool Held(KeyCode code)
        {
            var c = Control(code);
            return c != null && c.isPressed;
        }

        public static bool Down(KeyCode code)
        {
            var c = Control(code);
            return c != null && c.wasPressedThisFrame;
        }

        public static bool MouseHeld(int button)
        {
            var b = MouseButton(button);
            return b != null && b.isPressed;
        }

        public static bool MouseDown(int button)
        {
            var b = MouseButton(button);
            return b != null && b.wasPressedThisFrame;
        }

        /// <summary>Mouvement de la souris, à la même échelle que les axes « Mouse X/Y » de l'ancien système.</summary>
        public static Vector2 MouseDelta()
        {
            return Mouse.current != null ? Mouse.current.delta.ReadValue() * 0.1f : Vector2.zero;
        }

        /// <summary>Molette : -1, 0 ou 1 (l'amplitude brute varie selon la plateforme).</summary>
        public static float Scroll()
        {
            if (Mouse.current == null) return 0f;
            float y = Mouse.current.scroll.ReadValue().y;
            return Mathf.Abs(y) < 0.01f ? 0f : Mathf.Sign(y);
        }

        static UnityEngine.InputSystem.Controls.ButtonControl MouseButton(int button)
        {
            var m = Mouse.current;
            if (m == null) return null;
            switch (button)
            {
                case 0: return m.leftButton;
                case 1: return m.rightButton;
                default: return m.middleButton;
            }
        }

        static UnityEngine.InputSystem.Controls.KeyControl Control(KeyCode code)
        {
            var k = Keyboard.current;
            if (k == null) return null;
            Key key;
            if (code >= KeyCode.A && code <= KeyCode.Z) key = Key.A + (code - KeyCode.A);
            else if (code >= KeyCode.Alpha0 && code <= KeyCode.Alpha9) key = code == KeyCode.Alpha0 ? Key.Digit0 : Key.Digit1 + (code - KeyCode.Alpha1);
            else if (code >= KeyCode.Keypad0 && code <= KeyCode.Keypad9) key = Key.Numpad0 + (code - KeyCode.Keypad0);
            else
            {
                switch (code)
                {
                    case KeyCode.Space: key = Key.Space; break;
                    case KeyCode.Return: key = Key.Enter; break;
                    case KeyCode.KeypadEnter: key = Key.NumpadEnter; break;
                    case KeyCode.Escape: key = Key.Escape; break;
                    case KeyCode.LeftShift: key = Key.LeftShift; break;
                    case KeyCode.LeftControl: key = Key.LeftCtrl; break;
                    case KeyCode.F1: key = Key.F1; break;
                    case KeyCode.UpArrow: key = Key.UpArrow; break;
                    case KeyCode.DownArrow: key = Key.DownArrow; break;
                    case KeyCode.LeftArrow: key = Key.LeftArrow; break;
                    case KeyCode.RightArrow: key = Key.RightArrow; break;
                    default: return null;
                }
            }
            return k[key];
        }
#else
        /// <summary>Déplacement (x = droite, y = avant). L'ancien système suit la disposition du clavier :
        /// on accepte donc à la fois ZQSD (AZERTY) et WASD (QWERTY).</summary>
        public static Vector2 Move()
        {
            float x = (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow) ? 1f : 0f)
                    - (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.Q) || Input.GetKey(KeyCode.LeftArrow) ? 1f : 0f);
            float y = (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.Z) || Input.GetKey(KeyCode.UpArrow) ? 1f : 0f)
                    - (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow) ? 1f : 0f);
            return new Vector2(x, y);
        }

        public static bool Held(KeyCode code) { return Input.GetKey(code); }
        public static bool Down(KeyCode code) { return Input.GetKeyDown(code); }
        public static bool MouseHeld(int button) { return Input.GetMouseButton(button); }
        public static bool MouseDown(int button) { return Input.GetMouseButtonDown(button); }
        public static Vector2 MouseDelta() { return new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y")); }
        public static float Scroll() { return Mathf.Clamp(Input.mouseScrollDelta.y, -1f, 1f); }
#endif
    }
}
