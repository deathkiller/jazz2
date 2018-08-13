using Duality;
using Duality.Input;

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

        Count
    }

    public static class ControlScheme
    {
        public struct Mapping
        {
            public Key Key1;
            public Key Key2;

            public int GamepadIndex;
            public GamepadButton GamepadButton;
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
            const int maxSupportedPlayers = 1;

            mappings = new Mapping[maxSupportedPlayers * (int)PlayerActions.Count];

            for (int i = 0; i < mappings.Length; i++) {
                mappings[i].GamepadIndex = -1;
            }

            // ToDo
            mappings[(int)PlayerActions.Left].Key1 = Key.Left;
            mappings[(int)PlayerActions.Right].Key1 = Key.Right;
            mappings[(int)PlayerActions.Up].Key1 = Key.Up;
            mappings[(int)PlayerActions.Down].Key1 = Key.Down;
            mappings[(int)PlayerActions.Fire].Key1 = Key.Space;
            mappings[(int)PlayerActions.Jump].Key1 = Key.V;
            mappings[(int)PlayerActions.Run].Key1 = Key.C;
            mappings[(int)PlayerActions.SwitchWeapon].Key1 = Key.X;
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

            switch (action) {
                case PlayerActions.Left: return DualityApp.Keyboard.KeyPressed(Key.Left);
                case PlayerActions.Right: return DualityApp.Keyboard.KeyPressed(Key.Right);
                case PlayerActions.Up: return DualityApp.Keyboard.KeyPressed(Key.Up);
                case PlayerActions.Down: return DualityApp.Keyboard.KeyPressed(Key.Down);
                case PlayerActions.Fire: return DualityApp.Keyboard.KeyPressed(Key.Enter);
            }

            return false;
        }

        public static bool MenuActionHit(PlayerActions action)
        {
            if (isSuspended) {
                return false;
            }

            switch (action) {
                case PlayerActions.Left: return DualityApp.Keyboard.KeyHit(Key.Left);
                case PlayerActions.Right: return DualityApp.Keyboard.KeyHit(Key.Right);
                case PlayerActions.Up: return DualityApp.Keyboard.KeyHit(Key.Up);
                case PlayerActions.Down: return DualityApp.Keyboard.KeyHit(Key.Down);
                case PlayerActions.Fire: return DualityApp.Keyboard.KeyHit(Key.Enter);
            }

            return false;
        }

        public static bool PlayerActionPressed(int index, PlayerActions action)
        {
            if (isSuspended) {
                return false;
            }

            ref Mapping mapping = ref mappings[index * (int)PlayerActions.Count + (int)action];

            if (mapping.Key1 != Key.Unknown && DualityApp.Keyboard[mapping.Key1])
                return true;
            if (mapping.Key2 != Key.Unknown && DualityApp.Keyboard[mapping.Key2])
                return true;

            if (mapping.GamepadIndex != -1 && DualityApp.Gamepads[mapping.GamepadIndex][mapping.GamepadButton])
                return true;

            return false;
        }

        public static bool PlayerActionHit(int index, PlayerActions action)
        {
            if (isSuspended) {
                return false;
            }

            ref Mapping mapping = ref mappings[index * (int)PlayerActions.Count + (int)action];

            if (mapping.Key1 != Key.Unknown && DualityApp.Keyboard.KeyHit(mapping.Key1))
                return true;
            if (mapping.Key2 != Key.Unknown && DualityApp.Keyboard.KeyHit(mapping.Key2))
                return true;

            if (mapping.GamepadIndex != -1 && DualityApp.Gamepads[mapping.GamepadIndex].ButtonHit(mapping.GamepadButton))
                return true;

            return false;
        }
    }
}