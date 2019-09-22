using System.Runtime.CompilerServices;
using Duality;
using Duality.Input;
using Jazz2.Storage;

namespace Jazz2
{
    public enum PlayerActions
    {
        Left,
        Right,
        Up,
        Down,
        Fire,
        Jump,
        Run,
        SwitchWeapon,
        Menu,

        Count,
        
        None = -1
    }

    public static class ControlScheme
    {
        public struct Mapping
        {
            public Key Key1;
            public Key Key2;

            public int GamepadIndex;
            public GamepadButton GamepadButton;
            
#if ENABLE_TOUCH
            public bool TouchPressed;
            public bool TouchPressedLast;
#endif
        }

        private static Mapping[] mappings;
        private static bool isSuspended;

        public static bool IsSuspended
        {
            get { return isSuspended; }
            set { isSuspended = value; }
        }

        static ControlScheme()
        {
#if ENABLE_SPLITSCREEN
            const int maxSupportedPlayers = 4;
#else
            const int maxSupportedPlayers = 1;
#endif

            mappings = new Mapping[maxSupportedPlayers * (int)PlayerActions.Count];

            for (int i = 0; i < mappings.Length; i++) {
                mappings[i].GamepadIndex = -1;
            }

            // Default mappings
            // Player 1
            mappings[(int)PlayerActions.Left].Key1 = Key.Left;
            mappings[(int)PlayerActions.Right].Key1 = Key.Right;
            mappings[(int)PlayerActions.Up].Key1 = Key.Up;
            mappings[(int)PlayerActions.Down].Key1 = Key.Down;
            mappings[(int)PlayerActions.Fire].Key1 = Key.Space;
            mappings[(int)PlayerActions.Jump].Key1 = Key.V;
            mappings[(int)PlayerActions.Run].Key1 = Key.C;
            mappings[(int)PlayerActions.SwitchWeapon].Key1 = Key.X;
            mappings[(int)PlayerActions.Menu].Key1 = Key.Escape;
#if ENABLE_SPLITSCREEN
            // Player 2
            mappings[1 * (int)PlayerActions.Count + (int)PlayerActions.Left].Key1 = Key.Keypad4;
            mappings[1 * (int)PlayerActions.Count + (int)PlayerActions.Right].Key1 = Key.Keypad6;
            mappings[1 * (int)PlayerActions.Count + (int)PlayerActions.Up].Key1 = Key.Keypad8;
            mappings[1 * (int)PlayerActions.Count + (int)PlayerActions.Down].Key1 = Key.Keypad5;
            mappings[1 * (int)PlayerActions.Count + (int)PlayerActions.Fire].Key1 = Key.KeypadAdd;
            mappings[1 * (int)PlayerActions.Count + (int)PlayerActions.Jump].Key1 = Key.KeypadSubtract;
            mappings[1 * (int)PlayerActions.Count + (int)PlayerActions.Run].Key1 = Key.KeypadMultiply;
            mappings[1 * (int)PlayerActions.Count + (int)PlayerActions.SwitchWeapon].Key1 = Key.KeypadDivide;
#endif

            mappings[(int)PlayerActions.Left].GamepadIndex = 0;
            mappings[(int)PlayerActions.Right].GamepadIndex = 0;
            mappings[(int)PlayerActions.Up].GamepadIndex = 0;
            mappings[(int)PlayerActions.Down].GamepadIndex = 0;
            mappings[(int)PlayerActions.Fire].GamepadIndex = 0;
            mappings[(int)PlayerActions.Jump].GamepadIndex = 0;
            mappings[(int)PlayerActions.Run].GamepadIndex = 0;
            mappings[(int)PlayerActions.SwitchWeapon].GamepadIndex = 0;
            mappings[(int)PlayerActions.Menu].GamepadIndex = 0;

            mappings[(int)PlayerActions.Left].GamepadButton = GamepadButton.DPadLeft;
            mappings[(int)PlayerActions.Right].GamepadButton = GamepadButton.DPadRight;
            mappings[(int)PlayerActions.Up].GamepadButton = GamepadButton.DPadUp;
            mappings[(int)PlayerActions.Down].GamepadButton = GamepadButton.DPadDown;
            mappings[(int)PlayerActions.Fire].GamepadButton = GamepadButton.B;
            mappings[(int)PlayerActions.Jump].GamepadButton = GamepadButton.A;
            mappings[(int)PlayerActions.Run].GamepadButton = GamepadButton.X;
            mappings[(int)PlayerActions.SwitchWeapon].GamepadButton = GamepadButton.Y;
            mappings[(int)PlayerActions.Menu].GamepadButton = GamepadButton.Start;

            // Load saved mappings
            for (int i = 0; i < maxSupportedPlayers; i++) {
                int[] controls = Preferences.Get<int[]>("Controls_" + i);
                if (controls != null && controls.Length == (int)PlayerActions.Count * 4) {
                    for (int j = 0; j < (int)PlayerActions.Count; j++) {
                        int offset = i * (int)PlayerActions.Count;
                        ref Mapping mapping = ref mappings[offset + j];

                        mapping.Key1 = (Key)controls[j * 4 + 0];
                        mapping.Key2 = (Key)controls[j * 4 + 1];
                        mapping.GamepadIndex = controls[j * 4 + 2];
                        mapping.GamepadButton = (GamepadButton)controls[j * 4 + 3];
                    }
                }
            }
        }

        public static void SaveMappings()
        {
#if ENABLE_SPLITSCREEN
            const int maxSupportedPlayers = 2;
#else
            const int maxSupportedPlayers = 1;
#endif

            for (int i = 0; i < maxSupportedPlayers; i++) {
                int[] controls = new int[(int)PlayerActions.Count * 4];

                for (int j = 0; j < (int)PlayerActions.Count; j++) {
                    int offset = i * (int)PlayerActions.Count;
                    ref Mapping mapping = ref mappings[offset + j];

                    controls[j * 4 + 0] = (int)mapping.Key1;
                    controls[j * 4 + 1] = (int)mapping.Key2;
                    controls[j * 4 + 2] = (int)mapping.GamepadIndex;
                    controls[j * 4 + 3] = (int)mapping.GamepadButton;
                }

                Preferences.Set<int[]>("Controls_" + i, controls);
            }
        }

        public static ref Mapping GetCurrentMapping(int index, PlayerActions action)
        {
            return ref mappings[index * (int)PlayerActions.Count + (int)action];
        }

        public static bool MenuActionPressed(PlayerActions action)
        {
            if (isSuspended) {
                return false;
            }
            
#if ENABLE_TOUCH
            if (mappings[(int)action].TouchPressed) {
                return true;
            }
#endif

            bool keyPressed = false;
            switch (action) {
                case PlayerActions.Left: keyPressed = DualityApp.Keyboard.KeyPressed(Key.Left); break;
                case PlayerActions.Right: keyPressed = DualityApp.Keyboard.KeyPressed(Key.Right); break;
                case PlayerActions.Up: keyPressed = DualityApp.Keyboard.KeyPressed(Key.Up); break;
                case PlayerActions.Down: keyPressed = DualityApp.Keyboard.KeyPressed(Key.Down); break;
                case PlayerActions.Fire: keyPressed = DualityApp.Keyboard.KeyPressed(Key.Enter) || PlayerActionPressed(0, PlayerActions.Fire, false); break;
                case PlayerActions.Menu: keyPressed = DualityApp.Keyboard.KeyPressed(Key.Escape); break;
            }

            if (!keyPressed) {
                ref Mapping mapping = ref mappings[(int)action];
                if (mapping.GamepadIndex != -1) {
                    var gamepad = DualityApp.Gamepads[mapping.GamepadIndex];
                    switch (action) {
                        case PlayerActions.Left: keyPressed = gamepad.ButtonPressed(GamepadButton.DPadLeft) || gamepad.LeftThumbstick.X < -0.4f; break;
                        case PlayerActions.Right: keyPressed = gamepad.ButtonPressed(GamepadButton.DPadRight) || gamepad.LeftThumbstick.X > 0.4f; break;
                        case PlayerActions.Up: keyPressed = gamepad.ButtonPressed(GamepadButton.DPadUp) || gamepad.LeftThumbstick.Y < -0.4f; break;
                        case PlayerActions.Down: keyPressed = gamepad.ButtonPressed(GamepadButton.DPadDown) || gamepad.LeftThumbstick.Y > 0.4f; break;
                        case PlayerActions.Fire: keyPressed = gamepad.ButtonPressed(GamepadButton.A); break;
                        case PlayerActions.Menu: keyPressed = gamepad.ButtonPressed(GamepadButton.B); break;
                    }
                }
            }

            return keyPressed;
        }

        public static bool MenuActionHit(PlayerActions action)
        {
            if (isSuspended) {
                return false;
            }
            
#if ENABLE_TOUCH
            if (mappings[(int)action].TouchPressed && !mappings[(int)action].TouchPressedLast) {
                return true;
            }
#endif

            bool keyHit = false;
            switch (action) {
                case PlayerActions.Left: keyHit = DualityApp.Keyboard.KeyHit(Key.Left); break;
                case PlayerActions.Right: keyHit = DualityApp.Keyboard.KeyHit(Key.Right); break;
                case PlayerActions.Up: keyHit = DualityApp.Keyboard.KeyHit(Key.Up); break;
                case PlayerActions.Down: keyHit = DualityApp.Keyboard.KeyHit(Key.Down); break;
                case PlayerActions.Fire: keyHit = DualityApp.Keyboard.KeyHit(Key.Enter) || PlayerActionHit(0, PlayerActions.Fire, false); break;
                case PlayerActions.Menu: keyHit = DualityApp.Keyboard.KeyHit(Key.Escape); break;
            }

            if (!keyHit) {
                ref Mapping mapping = ref mappings[(int)action];
                if (mapping.GamepadIndex != -1) {
                    var gamepad = DualityApp.Gamepads[mapping.GamepadIndex];
                    switch (action) {
                        case PlayerActions.Left: keyHit = gamepad.ButtonHit(GamepadButton.DPadLeft) /*|| gamepad.LeftThumbstick.X < -0.4f*/; break;
                        case PlayerActions.Right: keyHit = gamepad.ButtonHit(GamepadButton.DPadRight) /*|| gamepad.LeftThumbstick.X > 0.4f*/; break;
                        case PlayerActions.Up: keyHit = gamepad.ButtonHit(GamepadButton.DPadUp) /*|| gamepad.LeftThumbstick.Y < -0.4f*/; break;
                        case PlayerActions.Down: keyHit = gamepad.ButtonHit(GamepadButton.DPadDown) /*|| gamepad.LeftThumbstick.Y > 0.4f*/; break;
                        case PlayerActions.Fire: keyHit = gamepad.ButtonHit(GamepadButton.A); break;
                        case PlayerActions.Menu: keyHit = gamepad.ButtonHit(GamepadButton.B); break;
                    }
                }
            }

            return keyHit;
        }

        public static bool PlayerActionPressed(int index, PlayerActions action, bool includeGamepads = true)
        {
            if (isSuspended) {
                return false;
            }

#if ENABLE_TOUCH
            if (index == 0 && mappings[(int)action].TouchPressed) {
                return true;
            }
#endif

            ref Mapping mapping = ref mappings[index * (int)PlayerActions.Count + (int)action];

            if (mapping.Key1 != Key.Unknown && DualityApp.Keyboard[mapping.Key1]) {
                return true;
            }
            if (mapping.Key2 != Key.Unknown && DualityApp.Keyboard[mapping.Key2]) {
                return true;
            }

            if (includeGamepads && mapping.GamepadIndex != -1 && DualityApp.Gamepads[mapping.GamepadIndex][mapping.GamepadButton]) {
                return true;
            }

            return false;
        }

        public static bool PlayerActionHit(int index, PlayerActions action, bool includeGamepads = true)
        {
            if (isSuspended) {
                return false;
            }

#if ENABLE_TOUCH
            if (index == 0 && mappings[(int)action].TouchPressed && !mappings[(int)action].TouchPressedLast) {
                return true;
            }
#endif

            ref Mapping mapping = ref mappings[index * (int)PlayerActions.Count + (int)action];

            if (mapping.Key1 != Key.Unknown && DualityApp.Keyboard.KeyHit(mapping.Key1)) {
                return true;
            }
            if (mapping.Key2 != Key.Unknown && DualityApp.Keyboard.KeyHit(mapping.Key2)) {
                return true;
            }

            if (includeGamepads && mapping.GamepadIndex != -1 && DualityApp.Gamepads[mapping.GamepadIndex].ButtonHit(mapping.GamepadButton)) {
                return true;
            }

            return false;
        }

        public static float PlayerMovement(int index)
        {
            if (isSuspended) {
                return 0f;
            }

#if ENABLE_TOUCH
            if (index == 0) {
                if (mappings[(int)PlayerActions.Right].TouchPressed) {
                    return 1f;
                } else if (mappings[(int)PlayerActions.Left].TouchPressed) {
                    return -1f;
                } 
            }
#endif

            ref Mapping mappingRight = ref mappings[index * (int)PlayerActions.Count + (int)PlayerActions.Right];

            if (mappingRight.Key1 != Key.Unknown && DualityApp.Keyboard[mappingRight.Key1]) {
                return 1f;
            }
            if (mappingRight.Key2 != Key.Unknown && DualityApp.Keyboard[mappingRight.Key2]) {
                return 1f;
            }

            ref Mapping mappingLeft = ref mappings[index * (int)PlayerActions.Count + (int)PlayerActions.Left];

            if (mappingLeft.Key1 != Key.Unknown && DualityApp.Keyboard[mappingLeft.Key1]) {
                return -1f;
            }
            if (mappingLeft.Key2 != Key.Unknown && DualityApp.Keyboard[mappingLeft.Key2]) {
                return -1f;
            }

            if (mappingRight.GamepadIndex != -1) {
                if (DualityApp.Gamepads[mappingRight.GamepadIndex][mappingRight.GamepadButton]) {
                    return 1f;
                } else if (DualityApp.Gamepads[mappingLeft.GamepadIndex][mappingLeft.GamepadButton]) {
                    return -1f;
                } else {
                    var gamepad = DualityApp.Gamepads[mappingRight.GamepadIndex];
                    float x = gamepad.LeftThumbstick.X;
                    return (MathF.Clamp((MathF.Abs(x) - 0.2f) / 0.7f, 0f, 1f) * MathF.Sign(x));
                }
            }

            return 0f;
        }

#if ENABLE_TOUCH
        internal static void UpdateTouchActions()
        {
            for (int i = 0; i < (int)PlayerActions.Count; i++) {
                mappings[i].TouchPressedLast = mappings[i].TouchPressed;
            }
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void InternalTouchAction(PlayerActions action, bool pressed)
        {
            mappings[(int)action].TouchPressed = pressed;
        }
#endif
    }
}