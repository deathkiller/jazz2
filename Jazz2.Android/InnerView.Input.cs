using System;
using System.IO;
using Android.Content.Res;
using Android.Views;
using Duality;
using Duality.Drawing;
using Duality.Input;
using Duality.Resources;
using Jazz2.Storage;

namespace Jazz2.Android
{
    partial class InnerView
    {
#if ENABLE_TOUCH
        public struct TouchButtonInfo
        {
            public PlayerActions Action;
            
            public float Left;
            public float Top;
            public float Width;
            public float Height;

            public ContentRef<Material> Material;

            public int CurrentPointerId;
        }

        internal static bool AllowVibrations = true;
        internal static bool ShowTouchButtons;
        internal static TouchButtonInfo[] TouchButtons;
        internal static float ControlsOpacity, LeftPadding, RightPadding, BottomPadding1, BottomPadding2;
#endif

        private bool[] pressedKeys = new bool[(int)Key.Last + 1];
        private bool[] pressedButtons = new bool[(int)(GamepadButton.Last + 1)];

        private void InitializeInput()
        {
#if ENABLE_TOUCH
            if (TouchButtons != null || viewportWidth == 0 || viewportHeight == 0) {
                // Game is not initialized yet or the buttons were already created
                return;
            }

            const float DpadLeft = 0.02f;
            const float DpadBottom = 0.1f;
            const float DpadThreshold = 0.09f;

            const float DpadSize = 0.37f;
            const float ButtonSize = 0.172f;
            const float SmallButtonSize = 0.098f;

            AssetManager assets = Context.Assets;

            Material matDpad = LoadButtonImageFromAssets(assets, "dpad.png");
            Material matFire = LoadButtonImageFromAssets(assets, "fire.png");
            Material matJump = LoadButtonImageFromAssets(assets, "jump.png");
            Material matRun = LoadButtonImageFromAssets(assets, "run.png");
            Material matSwitchWeapon = LoadButtonImageFromAssets(assets, "switch.png");

            TouchButtons = new[] {
                // D-pad
                CreateTouchButton(PlayerActions.None, matDpad, Alignment.BottomLeft, DpadLeft, DpadBottom, DpadSize, DpadSize),
                // D-pad subsections
                CreateTouchButton(PlayerActions.Left, null, Alignment.BottomLeft, DpadLeft - DpadThreshold, DpadBottom, (DpadSize / 3) + DpadThreshold, DpadSize),
                CreateTouchButton(PlayerActions.Right, null, Alignment.BottomLeft, DpadLeft + (DpadSize * 2 / 3), DpadBottom, (DpadSize / 3) + DpadThreshold, DpadSize),
                CreateTouchButton(PlayerActions.Up, null, Alignment.BottomLeft, DpadLeft, DpadBottom + (DpadSize * 2 / 3), DpadSize, (DpadSize / 3) + DpadThreshold),
                CreateTouchButton(PlayerActions.Down, null, Alignment.BottomLeft, DpadLeft, DpadBottom - DpadThreshold, DpadSize, (DpadSize / 3) + DpadThreshold),
                // Action buttons
                CreateTouchButton(PlayerActions.Fire, matFire, Alignment.BottomRight, (ButtonSize + 0.02f) * 2, 0.04f, ButtonSize, ButtonSize),
                CreateTouchButton(PlayerActions.Jump, matJump, Alignment.BottomRight, (ButtonSize + 0.02f), 0.04f + 0.08f, ButtonSize, ButtonSize),
                CreateTouchButton(PlayerActions.Run, matRun, Alignment.BottomRight, 0f, 0.01f + 0.15f, ButtonSize, ButtonSize),
                CreateTouchButton(PlayerActions.SwitchWeapon, matSwitchWeapon, Alignment.BottomRight, ButtonSize + 0.01f, 0.04f + 0.28f, SmallButtonSize, SmallButtonSize)
            };

            ShowTouchButtons = true;
            AllowVibrations = Preferences.Get("Vibrations", true);

            ControlsOpacity = Preferences.Get("ControlsOpacity", (byte)255) / 255f;
            LeftPadding = Preferences.Get("LeftPadding", (byte)20) * 0.001f;
            RightPadding = Preferences.Get("RightPadding", (byte)70) * 0.001f;
            BottomPadding1 = (Preferences.Get("BottomPadding1", (byte)128) - 128f) * 0.002f;
            BottomPadding2 = (Preferences.Get("BottomPadding2", (byte)128) - 128f) * 0.002f;
#endif

            DualityApp.Keyboard.Source = new KeyboardInputSource(this);
            DualityApp.Gamepads.ClearSources();
            DualityApp.Gamepads.AddSource(new GamepadInputSource(this));
        }

        public override bool OnTouchEvent(MotionEvent e)
        {
#if ENABLE_TOUCH
            if (TouchButtons != null) {
                ShowTouchButtons = true;

                MotionEventActions action = e.ActionMasked;
                if (action == MotionEventActions.Down || action == MotionEventActions.PointerDown) {
                    int pointerIndex = e.ActionIndex;
                    float x = e.GetX(pointerIndex) / (float)viewportWidth;
                    float y = e.GetY(pointerIndex) / (float)viewportHeight;
                    if (x < 0.5f) {
                        x -= LeftPadding;
                        y -= BottomPadding1;
                    } else {
                        x += RightPadding;
                        y -= BottomPadding2;
                    }

                    bool vibrated = false;
                    for (int i = 0; i < TouchButtons.Length; i++) {
                        ref TouchButtonInfo button = ref TouchButtons[i];
                        if (button.Action != PlayerActions.None) {
                            if (button.CurrentPointerId == -1 && IsOnButton(ref button, x, y)) {
                                int pointerId = e.GetPointerId(pointerIndex);

                                button.CurrentPointerId = pointerId;
                                ControlScheme.InternalTouchAction(button.Action, true);

                                if (AllowVibrations && !vibrated) {
                                    vibrator.Vibrate(16);
                                    vibrated = true;
                                }
                            }
                        }
                    }
                } else if (action == MotionEventActions.Move) {
                    int pointerCount = e.PointerCount;

                    for (int i = 0; i < TouchButtons.Length; i++) {
                        ref TouchButtonInfo button = ref TouchButtons[i];
                        if (button.Action != PlayerActions.None) {
                            if (button.CurrentPointerId != -1) {
                                int pointerIndex = e.FindPointerIndex(button.CurrentPointerId);

                                float x = e.GetX(pointerIndex) / (float)viewportWidth;
                                float y = e.GetY(pointerIndex) / (float)viewportHeight;
                                if (x < 0.5f) {
                                    x -= LeftPadding;
                                } else {
                                    x += RightPadding;
                                }

                                if (!IsOnButton(ref button, x, y)) {
                                    button.CurrentPointerId = -1;
                                    ControlScheme.InternalTouchAction(button.Action, false);
                                }
                            } else {
                                for (int j = 0; j < pointerCount; j++) {
                                    float x = e.GetX(j) / (float)viewportWidth;
                                    float y = e.GetY(j) / (float)viewportHeight;
                                    if (x < 0.5f) {
                                        x -= LeftPadding;
                                    } else {
                                        x += RightPadding;
                                    }

                                    if (IsOnButton(ref button, x, y)) {
                                        int pointerId = e.GetPointerId(j);

                                        button.CurrentPointerId = pointerId;
                                        ControlScheme.InternalTouchAction(button.Action, true);

                                        if (AllowVibrations) {
                                            vibrator.Vibrate(11);
                                        }
                                        break;
                                    }
                                }
                            }
                        }
                    }
                } else if (action == MotionEventActions.Up || action == MotionEventActions.Cancel) {
                    for (int i = 0; i < TouchButtons.Length; i++) {
                        ref TouchButtonInfo button = ref TouchButtons[i];
                        if (button.CurrentPointerId != -1) {
                            button.CurrentPointerId = -1;
                            ControlScheme.InternalTouchAction(button.Action, false);
                        }
                    }

                } else if (action == MotionEventActions.PointerUp) {
                    int pointerId = e.GetPointerId(e.ActionIndex);

                    for (int i = 0; i < TouchButtons.Length; i++) {
                        ref TouchButtonInfo button = ref TouchButtons[i];
                        if (button.CurrentPointerId == pointerId) {
                            button.CurrentPointerId = -1;
                            ControlScheme.InternalTouchAction(button.Action, false);
                        }
                    }
                }
            }
#endif

            //return base.OnTouchEvent(e);
            return true;
        }

#if ENABLE_TOUCH
        private bool IsOnButton(ref TouchButtonInfo button, float x, float y)
        {
            float left = button.Left;
            if (x < left) return false;

            float top = button.Top;
            if (y < top) return false;

            float right = left + button.Width;
            if (x > right) return false;

            float bottom = top + button.Height;
            if (y > bottom) return false;

            return true;
        }
#endif

        public override bool OnKeyDown(Keycode keyCode, KeyEvent e)
        {
            if (keyCode == Keycode.Menu || keyCode == Keycode.VolumeDown || keyCode == Keycode.VolumeUp || keyCode == Keycode.VolumeMute) {
                // Nothing to do...
            } else if (keyCode == Keycode.Back) {
                ControlScheme.InternalTouchAction(PlayerActions.Menu, true);
                return true;
            } else {
                ShowTouchButtons = false;

                GamepadButton gamepadButton = ToDualityButton(keyCode);
                if (gamepadButton != (GamepadButton)(-1)) {
                    pressedButtons[(int)gamepadButton] = true;
                } else {
                    pressedKeys[(int)ToDualityKey(keyCode)] = true;
                }

                return true;
            }

            return base.OnKeyDown(keyCode, e);
        }

        public override bool OnKeyUp(Keycode keyCode, KeyEvent e)
        {
            if (keyCode == Keycode.Back) {
                ControlScheme.InternalTouchAction(PlayerActions.Menu, false);
                return true;
            }

            GamepadButton gamepadButton = ToDualityButton(keyCode);
            if (gamepadButton != (GamepadButton)(-1)) {
                pressedButtons[(int)gamepadButton] = false;
            } else {
                pressedKeys[(int)ToDualityKey(keyCode)] = false;
            }

            return true;
        }

#if ENABLE_TOUCH
        private TouchButtonInfo CreateTouchButton(PlayerActions action, Material material, Alignment alignment, float x, float y, float w, float h)
        {
            float toWidth = ((float)viewportHeight / viewportWidth);
            float left, top, width, height;

            width = w * toWidth;
            if ((alignment & Alignment.Right) != 0) {
                left = 1f - x * toWidth - width;
            } else {
                left = x * toWidth;
            }

            height = h;
            if ((alignment & Alignment.Bottom) != 0) {
                top = 1f - y - height;
            } else {
                top = y;
            }

            return new TouchButtonInfo {
                Action = action,
                Left = left,
                Top = top,
                Width = width,
                Height = height,
                Material = material,
                CurrentPointerId = -1
            };
        }

        private static Material LoadButtonImageFromAssets(AssetManager assets, string filename)
        {
            using (Stream s = assets.Open(filename))
            using (MemoryStream ms = new MemoryStream()) {
                // ToDo: Workaround for System.NotSupportedException in Android.Runtime.InputStreamInvoker.Position
                s.CopyTo(ms);
                ms.Position = 0;

                return new Material(DrawTechnique.Alpha, new Texture(new Pixmap(new Png(ms).GetPixelData()), TextureSizeMode.NonPowerOfTwo));
            }
        }
#endif

        private static Key ToDualityKey(Keycode key)
        {
            switch (key) {
                case Keycode.ShiftLeft: return Key.ShiftLeft;
                case Keycode.ShiftRight: return Key.ShiftRight;
                case Keycode.CtrlLeft: return Key.ControlLeft;
                case Keycode.CtrlRight: return Key.ControlRight;
                case Keycode.AltLeft: return Key.AltLeft;
                case Keycode.AltRight: return Key.AltRight;
                case Keycode.MetaLeft: return Key.WinLeft;
                case Keycode.MetaRight: return Key.WinRight;
                case Keycode.Menu: return Key.Menu;

                case Keycode.Enter: return Key.Enter;
                case Keycode.Escape: return Key.Escape;
                case Keycode.Space: return Key.Space;
                case Keycode.Tab: return Key.Tab;
                case Keycode.Del: return Key.BackSpace;
                case Keycode.Insert: return Key.Insert;
                case Keycode.ForwardDel: return Key.Delete;
                case Keycode.PageUp: return Key.PageUp;
                case Keycode.PageDown: return Key.PageDown;
                case Keycode.MoveHome: return Key.Home;
                case Keycode.MoveEnd: return Key.End;
                case Keycode.CapsLock: return Key.CapsLock;
                case Keycode.ScrollLock: return Key.ScrollLock;
                case Keycode.Sysrq: return Key.PrintScreen;
                case Keycode.Break: return Key.Pause;
                case Keycode.NumLock: return Key.NumLock;
                case Keycode.Clear: return Key.Clear;
                case Keycode.Sleep: return Key.Sleep;

                //case Keycode.Tilde: return Key.Tilde;
                case Keycode.Minus: return Key.Minus;
                case Keycode.Plus: return Key.Plus;
                case Keycode.LeftBracket: return Key.BracketLeft;
                case Keycode.RightBracket: return Key.BracketRight;
                case Keycode.Semicolon: return Key.Semicolon;
                //case Keycode.Quote: return Key.Quote;
                case Keycode.Comma: return Key.Comma;
                case Keycode.Period: return Key.Period;
                case Keycode.Slash: return Key.Slash;
                case Keycode.Backslash: return Key.BackSlash;
                //case Keycode.NonUSBackSlash: return Key.NonUSBackSlash;
            }

            if (key >= Keycode.A && key <= Keycode.Z) {
                return key - Keycode.A + Key.A;
            }
            if (key >= Keycode.F1 && key <= Keycode.F12) {
                return key - Keycode.F1 + Key.F1;
            }
            if (key >= Keycode.Numpad0 && key <= Keycode.NumpadEnter) {
                return key - Keycode.Numpad0 + Key.Keypad0;
            }
            if (key >= Keycode.Num0 && key <= Keycode.Num9) {
                return key - Keycode.Num0 + Key.Number0;
            }

            return Key.Unknown;
        }

        private static GamepadButton ToDualityButton(Keycode key)
        {
            switch (key) {
                case Keycode.DpadUp: return GamepadButton.DPadUp;
                case Keycode.DpadDown: return GamepadButton.DPadDown;
                case Keycode.DpadLeft: return GamepadButton.DPadLeft;
                case Keycode.DpadRight: return GamepadButton.DPadRight;

                case Keycode.ButtonA: return GamepadButton.A;
                case Keycode.ButtonB: return GamepadButton.B;
                case Keycode.ButtonX: return GamepadButton.X;
                case Keycode.ButtonY: return GamepadButton.Y;

                case Keycode.ButtonL1: return GamepadButton.LeftShoulder;
                case Keycode.ButtonR1: return GamepadButton.RightShoulder;
                case Keycode.ButtonThumbl: return GamepadButton.LeftStick;
                case Keycode.ButtonThumbr: return GamepadButton.RightStick;

                case Keycode.ButtonStart: return GamepadButton.Start;
                case Keycode.ButtonSelect: return GamepadButton.Back;

                default: return (GamepadButton)(-1);
            }
        }

        private class KeyboardInputSource : IKeyboardInputSource
        {
            private readonly InnerView owner;

            public bool this[Key key] => owner.pressedKeys[(int)key];

            public string CharInput => "";

            string IUserInputSource.Id => "Android Keyboard Provider";
            string IUserInputSource.ProductName => "Android Keyboard Provider";
            Guid IUserInputSource.ProductId => new Guid("0E57FFB8-2D48-4447-B34E-5AA062C824AB");

            bool IUserInputSource.IsAvailable => true;

            public KeyboardInputSource(InnerView owner)
            {
                this.owner = owner;
            }

            public void UpdateState()
            {
                // Nothing to do...
            }
        }

        private class GamepadInputSource : IGamepadInputSource
        {
            private readonly InnerView owner;

            public float this[GamepadAxis axis]
            {
                get
                {
                    return 0f;
                }
            }

            public bool this[GamepadButton button]
            {
                get
                {
                    return owner.pressedButtons[(int)button];
                }
            }

            string IUserInputSource.Id => "Android Gamepad Provider";
            string IUserInputSource.ProductName => "Android Gamepad Provider";
            Guid IUserInputSource.ProductId => new Guid("BCD8F847-79B4-410C-AF4C-B89E5A3E60F7");

            bool IUserInputSource.IsAvailable => true;

            public GamepadInputSource(InnerView owner)
            {
                this.owner = owner;
            }

            public void UpdateState()
            {
                // Nothing to do...
            }

            public void SetVibration(float left, float right)
            {
                
            }
        }
    }
}