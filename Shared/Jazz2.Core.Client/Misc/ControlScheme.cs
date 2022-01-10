using System;
using System.Runtime.CompilerServices;
using Duality;
using Duality.Input;
using Jazz2.Storage;
using MathF = Duality.MathF;

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
        public struct Mapping : IEquatable<Mapping>
        {
            public Key Key1;
            public Key Key2;

            public int GamepadIndex;
            public GamepadButton GamepadButton;

#if ENABLE_TOUCH
            public bool TouchPressed;
            public bool TouchPressedLast;
#endif

            public override bool Equals(object obj)
            {
                return obj is Mapping mapping && Equals(mapping);
            }

            public bool Equals(Mapping other)
            {
                return Key1 == other.Key1 &&
                       Key2 == other.Key2 &&
                       GamepadIndex == other.GamepadIndex &&
                       GamepadButton == other.GamepadButton;
            }

            public override int GetHashCode()
            {
                int hashCode = 290702377;
                hashCode = hashCode * -1521134295 + Key1.GetHashCode();
                hashCode = hashCode * -1521134295 + Key2.GetHashCode();
                hashCode = hashCode * -1521134295 + GamepadIndex.GetHashCode();
                hashCode = hashCode * -1521134295 + GamepadButton.GetHashCode();
                return hashCode;
            }

            public static bool operator ==(Mapping left, Mapping right)
            {
                return left.Equals(right);
            }

            public static bool operator !=(Mapping left, Mapping right)
            {
                return !(left == right);
            }
        }

        private static Mapping[] mappings;
        private static bool isSuspended;
        private static bool[] analogPressed = new bool[4];
        private static bool[] analogPressedPrev = new bool[4];

        private static bool freezeAnalogEnable;
        private static float freezeAnalogH, freezeAnalogV;

        public static bool IsSuspended
        {
            get { return isSuspended; }
            set { isSuspended = value; }
        }

        static ControlScheme()
        {
#if ENABLE_SPLITSCREEN
            const int MaxSupportedPlayers = 4;
#else
            const int MaxSupportedPlayers = 1;
#endif

            mappings = new Mapping[MaxSupportedPlayers * (int)PlayerActions.Count];

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
            mappings[(int)PlayerActions.Fire].GamepadButton = GamepadButton.X;
            mappings[(int)PlayerActions.Jump].GamepadButton = GamepadButton.A;
            mappings[(int)PlayerActions.Run].GamepadButton = GamepadButton.B;
            mappings[(int)PlayerActions.SwitchWeapon].GamepadButton = GamepadButton.Y;
            mappings[(int)PlayerActions.Menu].GamepadButton = GamepadButton.Start;

            // Load saved mappings
            for (int i = 0; i < MaxSupportedPlayers; i++) {
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
            const int MaxSupportedPlayers = 2;
#else
            const int MaxSupportedPlayers = 1;
#endif

            for (int i = 0; i < MaxSupportedPlayers; i++) {
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
                if (action <= PlayerActions.Down && analogPressed[(int)action]) {
                    keyPressed = true;
                } else {
                    ref Mapping mapping = ref mappings[(int)action];
                    if (mapping.GamepadIndex != -1) {
                        var gamepad = DualityApp.Gamepads[mapping.GamepadIndex];
                        switch (action) {
                            case PlayerActions.Left: keyPressed = gamepad.ButtonPressed(GamepadButton.DPadLeft); break;
                            case PlayerActions.Right: keyPressed = gamepad.ButtonPressed(GamepadButton.DPadRight); break;
                            case PlayerActions.Up: keyPressed = gamepad.ButtonPressed(GamepadButton.DPadUp); break;
                            case PlayerActions.Down: keyPressed = gamepad.ButtonPressed(GamepadButton.DPadDown); break;
                            case PlayerActions.Fire: keyPressed = gamepad.ButtonPressed(GamepadButton.A); break;
                            case PlayerActions.Menu: keyPressed = gamepad.ButtonPressed(GamepadButton.B); break;
                        }
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
                if (action <= PlayerActions.Down && !analogPressedPrev[(int)action] && analogPressed[(int)action]) {
                    keyHit = true;
                } else {
                    ref Mapping mapping = ref mappings[(int)action];
                    if (mapping.GamepadIndex != -1) {
                        var gamepad = DualityApp.Gamepads[mapping.GamepadIndex];
                        switch (action) {
                            case PlayerActions.Left: keyHit = gamepad.ButtonHit(GamepadButton.DPadLeft); break;
                            case PlayerActions.Right: keyHit = gamepad.ButtonHit(GamepadButton.DPadRight); break;
                            case PlayerActions.Up: keyHit = gamepad.ButtonHit(GamepadButton.DPadUp); break;
                            case PlayerActions.Down: keyHit = gamepad.ButtonHit(GamepadButton.DPadDown); break;
                            case PlayerActions.Fire: keyHit = gamepad.ButtonHit(GamepadButton.A); break;
                            case PlayerActions.Menu: keyHit = gamepad.ButtonHit(GamepadButton.B); break;
                        }
                    }
                }
            }

            return keyHit;
        }

        public static bool PlayerActionPressed(int index, PlayerActions action, bool includeGamepads = true)
        {
            return PlayerActionPressed(index, action, includeGamepads, out _);
        }

        public static bool PlayerActionPressed(int index, PlayerActions action, bool includeGamepads, out bool isGamepad)
        {
            isGamepad = false;

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

            if (includeGamepads && mapping.GamepadIndex != -1) {
                if (DualityApp.Gamepads[mapping.GamepadIndex][mapping.GamepadButton]) {
                    isGamepad = true;
                    return true;
                }
                
                switch (action) {
                    case PlayerActions.Left: if ((!freezeAnalogEnable || index != 0) && DualityApp.Gamepads[mapping.GamepadIndex].LeftThumbstick.X < -0.8f) { isGamepad = true; return true; } break;
                    case PlayerActions.Right: if ((!freezeAnalogEnable || index != 0) && DualityApp.Gamepads[mapping.GamepadIndex].LeftThumbstick.X > 0.8f) { isGamepad = true; return true; } break;
                    case PlayerActions.Up: if ((!freezeAnalogEnable || index != 0) && DualityApp.Gamepads[mapping.GamepadIndex].LeftThumbstick.Y < -0.8f) { isGamepad = true; return true; } break;
                    case PlayerActions.Down: if ((!freezeAnalogEnable || index != 0) && DualityApp.Gamepads[mapping.GamepadIndex].LeftThumbstick.Y > 0.8f) { isGamepad = true; return true; } break;
                    case PlayerActions.Run: if (DualityApp.Gamepads[mapping.GamepadIndex].LeftTrigger > 0.5f) { isGamepad = true; return true; } break;
                    case PlayerActions.Fire: if (DualityApp.Gamepads[mapping.GamepadIndex].RightTrigger > 0.5f) { isGamepad = true; return true; } break;
                }
            }

            return false;
        }

        public static bool PlayerActionHit(int index, PlayerActions action, bool includeGamepads = true)
        {
            return PlayerActionHit(index, action, includeGamepads, out _);
        }

        public static bool PlayerActionHit(int index, PlayerActions action, bool includeGamepads, out bool isGamepad)
        {
            isGamepad = false;

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
                isGamepad = true;
                return true;
            }

            return false;
        }

        public static float PlayerHorizontalMovement(int index)
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
                } else if (freezeAnalogEnable && index == 0) {
                    return freezeAnalogH;
                } else {
                    var gamepad = DualityApp.Gamepads[mappingRight.GamepadIndex];
                    float x = gamepad.LeftThumbstick.X;
                    return (MathF.Clamp((MathF.Abs(x) - 0.2f) / 0.7f, 0f, 1f) * MathF.Sign(x));
                }
            }

            return 0f;
        }

        public static float PlayerVerticalMovement(int index)
        {
            if (isSuspended) {
                return 0f;
            }

#if ENABLE_TOUCH
            if (index == 0) {
                if (mappings[(int)PlayerActions.Down].TouchPressed) {
                    return 1f;
                } else if (mappings[(int)PlayerActions.Up].TouchPressed) {
                    return -1f;
                } 
            }
#endif

            ref Mapping mappingDown = ref mappings[index * (int)PlayerActions.Count + (int)PlayerActions.Down];

            if (mappingDown.Key1 != Key.Unknown && DualityApp.Keyboard[mappingDown.Key1]) {
                return 1f;
            }
            if (mappingDown.Key2 != Key.Unknown && DualityApp.Keyboard[mappingDown.Key2]) {
                return 1f;
            }

            ref Mapping mappingUp = ref mappings[index * (int)PlayerActions.Count + (int)PlayerActions.Up];

            if (mappingUp.Key1 != Key.Unknown && DualityApp.Keyboard[mappingUp.Key1]) {
                return -1f;
            }
            if (mappingUp.Key2 != Key.Unknown && DualityApp.Keyboard[mappingUp.Key2]) {
                return -1f;
            }

            if (mappingDown.GamepadIndex != -1) {
                if (DualityApp.Gamepads[mappingDown.GamepadIndex][mappingDown.GamepadButton]) {
                    return 1f;
                } else if (DualityApp.Gamepads[mappingUp.GamepadIndex][mappingUp.GamepadButton]) {
                    return -1f;
                } else if (freezeAnalogEnable && index == 0) {
                    return freezeAnalogV;
                } else {
                    var gamepad = DualityApp.Gamepads[mappingDown.GamepadIndex];
                    float y = gamepad.LeftThumbstick.Y;
                    return (MathF.Clamp((MathF.Abs(y) - 0.2f) / 0.7f, 0f, 1f) * MathF.Sign(y));
                }
            }

            return 0f;
        }

        public static void EnableFreezeAnalog(int index, bool enable)
        {
            if (index != 0 && freezeAnalogEnable == enable) {
                return;
            }

            if (enable) {
                freezeAnalogH = PlayerHorizontalMovement(index);
                freezeAnalogV = PlayerVerticalMovement(index);
            }

            freezeAnalogEnable = enable;
        }

        public static void GetWeaponWheel(int index, out float x, out float y)
        {
            x = 0f;
            y = 0f;

            if (isSuspended) {
                return;
            }

            ref Mapping mappingRight = ref mappings[index * (int)PlayerActions.Count + (int)PlayerActions.Right];

            if (mappingRight.GamepadIndex != -1) {
                var gamepad = DualityApp.Gamepads[mappingRight.GamepadIndex];
                float rx = gamepad.LeftThumbstick.X;
                float ry = gamepad.LeftThumbstick.Y;
                if (MathF.Abs(rx) < 0.5f && MathF.Abs(ry) < 0.5f) {
                    return;
                }

                x = rx;
                y = ry;
            }
        }

        internal static void UpdateAnalogPressed()
        {
            ref Mapping mappingUp = ref mappings[(int)PlayerActions.Up];
            ref Mapping mappingDown = ref mappings[(int)PlayerActions.Down];
            ref Mapping mappingLeft = ref mappings[(int)PlayerActions.Left];
            ref Mapping mappingRight = ref mappings[(int)PlayerActions.Right];

            if (mappingUp.GamepadIndex == -1 ||
                mappingUp.GamepadIndex != mappingDown.GamepadIndex ||
                mappingUp.GamepadIndex != mappingLeft.GamepadIndex ||
                mappingUp.GamepadIndex != mappingRight.GamepadIndex) {
                return;
            }

            for (int i = 0; i < analogPressed.Length; i++) {
                analogPressedPrev[i] = analogPressed[i];
            }

            var gamepad = DualityApp.Gamepads[mappingUp.GamepadIndex];

            float x = gamepad.LeftThumbstick.X;
            float y = gamepad.LeftThumbstick.Y;

            analogPressed[(int)PlayerActions.Up] = (y < -0.5f);
            analogPressed[(int)PlayerActions.Down] = (y > 0.5f);
            analogPressed[(int)PlayerActions.Left] = (x < -0.5f);
            analogPressed[(int)PlayerActions.Right] = (x > 0.5f);
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