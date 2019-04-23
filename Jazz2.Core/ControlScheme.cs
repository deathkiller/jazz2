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

            switch (action) {
                case PlayerActions.Left: return DualityApp.Keyboard.KeyPressed(Key.Left);
                case PlayerActions.Right: return DualityApp.Keyboard.KeyPressed(Key.Right);
                case PlayerActions.Up: return DualityApp.Keyboard.KeyPressed(Key.Up);
                case PlayerActions.Down: return DualityApp.Keyboard.KeyPressed(Key.Down);
                case PlayerActions.Fire: return DualityApp.Keyboard.KeyPressed(Key.Enter);
                case PlayerActions.Menu: return DualityApp.Keyboard.KeyPressed(Key.Escape);
            }

            return false;
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

            switch (action) {
                case PlayerActions.Left: return DualityApp.Keyboard.KeyHit(Key.Left);
                case PlayerActions.Right: return DualityApp.Keyboard.KeyHit(Key.Right);
                case PlayerActions.Up: return DualityApp.Keyboard.KeyHit(Key.Up);
                case PlayerActions.Down: return DualityApp.Keyboard.KeyHit(Key.Down);
                case PlayerActions.Fire: return DualityApp.Keyboard.KeyHit(Key.Enter);
                case PlayerActions.Menu: return DualityApp.Keyboard.KeyHit(Key.Escape);
            }

            return false;
        }

        public static bool PlayerActionPressed(int index, PlayerActions action)
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

            if (mapping.GamepadIndex != -1 && DualityApp.Gamepads[mapping.GamepadIndex][mapping.GamepadButton]) {
                return true;
            }

            return false;
        }

        public static bool PlayerActionHit(int index, PlayerActions action)
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

            if (mapping.GamepadIndex != -1 && DualityApp.Gamepads[mapping.GamepadIndex].ButtonHit(mapping.GamepadButton)) {
                return true;
            }

            return false;
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