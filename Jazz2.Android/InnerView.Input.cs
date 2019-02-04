using System;
using System.IO;
using Android.Content.Res;
using Android.Views;
using Duality.Drawing;
using Duality.Input;
using Duality.Resources;
using Jazz2.Storage;

namespace Duality.Android
{
    partial class InnerView
    {
        public struct VirtualButton
        {
            public Key KeyCode;
            public float Left;
            public float Top;
            public float Width;
            public float Height;

            public ContentRef<Material> Material;

            public int CurrentPointerId;
        }

        internal static bool allowVibrations = true;
        internal static bool showVirtualButtons;
        internal static VirtualButton[] virtualButtons;

        private bool[] pressedButtons = new bool[(int)Key.Last + 1];

        private void InitializeInput()
        {
            if (virtualButtons != null) {
                // It's already initialized...
                return;
            }

            DualityApp.Keyboard.Source = new KeyboardInputSource(this);
            //DualityApp.Gamepads.AddSource(new GamepadInputSource(this));

            const float dpadLeft = 0.02f;
            const float dpadTop = 0.58f;
            const float dpadWidth = 0.2f;
            const float dpadHeight = 0.37f;
            const float dpadThresholdX = 0.05f;
            const float dpadThresholdY = 0.09f;

            IImageCodec imageCodec = ImageCodec.GetRead(ImageCodec.FormatPng);
            AssetManager assets = Context.Assets;

            Material matDpad, matFire, matJump, matRun, matSwitchWeapon;
            using (Stream s = assets.Open("dpad.png")) {
                matDpad = new Material(DrawTechnique.Alpha, new Texture(new Pixmap(imageCodec.Read(s)), TextureSizeMode.NonPowerOfTwo));
            }
            using (Stream s = assets.Open("fire.png")) {
                matFire = new Material(DrawTechnique.Alpha, new Texture(new Pixmap(imageCodec.Read(s)), TextureSizeMode.NonPowerOfTwo));
            }
            using (Stream s = assets.Open("jump.png")) {
                matJump = new Material(DrawTechnique.Alpha, new Texture(new Pixmap(imageCodec.Read(s)), TextureSizeMode.NonPowerOfTwo));
            }
            using (Stream s = assets.Open("run.png")) {
                matRun = new Material(DrawTechnique.Alpha, new Texture(new Pixmap(imageCodec.Read(s)), TextureSizeMode.NonPowerOfTwo));
            }
            using (Stream s = assets.Open("switch.png")) {
                matSwitchWeapon = new Material(DrawTechnique.Alpha, new Texture(new Pixmap(imageCodec.Read(s)), TextureSizeMode.NonPowerOfTwo));
            }

            virtualButtons = new[] {
                new VirtualButton { Left = dpadLeft, Top = dpadTop, Width = dpadWidth, Height = dpadHeight, Material = matDpad, CurrentPointerId = -1 },

                new VirtualButton { KeyCode = Key.Left, Left = dpadLeft - dpadThresholdX, Top = dpadTop, Width = (dpadWidth / 3) + dpadThresholdX, Height = dpadHeight, CurrentPointerId = -1 },
                new VirtualButton { KeyCode = Key.Right, Left = (dpadLeft + (dpadWidth * 2 / 3)), Top = dpadTop, Width = (dpadWidth / 3) + dpadThresholdX, Height = dpadHeight, CurrentPointerId = -1 },
                new VirtualButton { KeyCode = Key.Up, Left = dpadLeft, Top = dpadTop - dpadThresholdY, Width = dpadWidth, Height = (dpadHeight / 3) + dpadThresholdY, CurrentPointerId = -1 },
                new VirtualButton { KeyCode = Key.Down, Left = dpadLeft, Top = (dpadTop + (dpadHeight * 2 / 3)), Width = dpadWidth, Height = (dpadHeight / 3) + dpadThresholdY, CurrentPointerId = -1 },

                new VirtualButton { KeyCode = Key.Space, Left = 0.68f, Top = 0.79f, Width = 0.094f, Height = 0.168f, Material = matFire, CurrentPointerId = -1 },
                new VirtualButton { KeyCode = Key.V, Left = 0.785f, Top = 0.71f, Width = 0.094f, Height = 0.168f, Material = matJump, CurrentPointerId = -1 },
                new VirtualButton { KeyCode = Key.C, Left = 0.89f, Top = 0.64f, Width = 0.094f, Height = 0.168f, Material = matRun, CurrentPointerId = -1 },
                new VirtualButton { KeyCode = Key.X, Left = 0.83f, Top = 0.57f, Width = 0.055f, Height = 0.096f, Material = matSwitchWeapon, CurrentPointerId = -1 },

#if DEBUG
                new VirtualButton { KeyCode = Key.D, Left = 0.8f, Top = 0.1f, Width = 0.06f, Height = 0.1f, CurrentPointerId = -1 },
                new VirtualButton { KeyCode = Key.N, Left = 0.9f, Top = 0.1f, Width = 0.08f, Height = 0.16f, CurrentPointerId = -1 },
#endif

                new VirtualButton { KeyCode = Key.Enter, Left = 0.68f, Top = 0.79f, Width = 0.094f, Height = 0.17f, CurrentPointerId = -1 },
                new VirtualButton { KeyCode = Key.Enter, Left = 0.785f, Top = 0.71f, Width = 0.094f, Height = 0.17f, CurrentPointerId = -1 },
                new VirtualButton { KeyCode = Key.Enter, Left = 0.89f, Top = 0.64f, Width = 0.094f, Height = 0.17f, CurrentPointerId = -1 },
            };

            showVirtualButtons = true;
            allowVibrations = Preferences.Get("Vibrations", true);
        }

        public override bool OnTouchEvent(MotionEvent e)
        {
            if (virtualButtons != null) {
                showVirtualButtons = true;

                MotionEventActions action = e.ActionMasked;
                if (action == MotionEventActions.Down || action == MotionEventActions.PointerDown) {
                    int pointerIndex = e.ActionIndex;
                    float x = e.GetX(pointerIndex);
                    float y = e.GetY(pointerIndex);
                    float w = e.GetTouchMajor(pointerIndex) * 0.5f;
                    float h = e.GetTouchMinor(pointerIndex) * 0.5f;

                    bool vibrated = false;
                    for (int i = 0; i < virtualButtons.Length; i++) {
                        ref VirtualButton button = ref virtualButtons[i];
                        if (button.KeyCode != Key.Unknown) {
                            if (button.CurrentPointerId == -1 && IsOnButton(ref button, x, y, w, h)) {
                                int pointerId = e.GetPointerId(pointerIndex);

                                button.CurrentPointerId = pointerId;
                                pressedButtons[(int)button.KeyCode] = true;

                                if (allowVibrations && !vibrated) {
                                    vibrator.Vibrate(16);
                                    vibrated = true;
                                }
                            }
                        }
                    }
                } else if (action == MotionEventActions.Move) {
                    int pointerCount = e.PointerCount;

                    for (int i = 0; i < virtualButtons.Length; i++) {
                        ref VirtualButton button = ref virtualButtons[i];
                        if (button.KeyCode != Key.Unknown) {
                            if (button.CurrentPointerId != -1) {
                                int pointerIndex = e.FindPointerIndex(button.CurrentPointerId);

                                if (!IsOnButton(ref button, e.GetX(pointerIndex), e.GetY(pointerIndex), e.GetTouchMajor(pointerIndex) * 0.5f, e.GetTouchMinor(pointerIndex) * 0.5f)) {
                                    button.CurrentPointerId = -1;
                                    pressedButtons[(int)button.KeyCode] = false;
                                }
                            } else {
                                for (int j = 0; j < pointerCount; j++) {
                                    if (IsOnButton(ref button, e.GetX(j), e.GetY(j), e.GetTouchMajor(j) * 0.5f, e.GetTouchMinor(j) * 0.5f)) {
                                        int pointerId = e.GetPointerId(j);

                                        button.CurrentPointerId = pointerId;
                                        pressedButtons[(int)button.KeyCode] = true;

                                        if (allowVibrations) {
                                            vibrator.Vibrate(11);
                                        }
                                        break;
                                    }
                                }
                            }
                        }
                    }
                } else if (action == MotionEventActions.Up || action == MotionEventActions.Cancel) {
                    for (int i = 0; i < virtualButtons.Length; i++) {
                        ref VirtualButton button = ref virtualButtons[i];
                        if (button.CurrentPointerId != -1) {
                            button.CurrentPointerId = -1;
                            pressedButtons[(int)button.KeyCode] = false;
                        }
                    }

                } else if (action == MotionEventActions.PointerUp) {
                    int pointerId = e.GetPointerId(e.ActionIndex);

                    for (int i = 0; i < virtualButtons.Length; i++) {
                        ref VirtualButton button = ref virtualButtons[i];

                        if (button.CurrentPointerId == pointerId) {
                            button.CurrentPointerId = -1;
                            pressedButtons[(int)button.KeyCode] = false;
                        }
                    }
                }
            }

            //return base.OnTouchEvent(e);
            return true;
        }

        private bool IsOnButton(ref VirtualButton button, float x, float y, float rw, float rh)
        {
            float left = button.Left * viewportWidth;
            if (x - rw < left)
                return false;

            float top = button.Top * viewportHeight;
            if (y - rh < top)
                return false;

            float right = left + button.Width * viewportWidth;
            if (x + rw > right)
                return false;

            float bottom = top + button.Height * viewportHeight;
            if (y + rh > bottom)
                return false;

            return true;
        }

        public override bool OnKeyDown(Keycode keyCode, KeyEvent e)
        {
            if (keyCode == Keycode.Menu || keyCode == Keycode.VolumeDown || keyCode == Keycode.VolumeUp || keyCode == Keycode.VolumeMute) {
                /*if (menu.IsShowing)
                    menu.Dismiss();
                else
                    menu.Show();

                return true;*/
            } else if (keyCode == Keycode.Back) {
                pressedButtons[(int)Key.Escape] = true;
                return true;
            } else {
                showVirtualButtons = false;
                pressedButtons[(int)ToDuality(keyCode)] = true;

                // ToDo: Remove this... gamepad to keyboard redirection
                if (keyCode == Keycode.Button1) {
                    pressedButtons[(int)Key.ShiftLeft] = true;
                } else if (keyCode == Keycode.Button2) {
                    pressedButtons[(int)Key.ControlLeft] = true;
                } else if (keyCode == Keycode.Button3) {
                    pressedButtons[(int)Key.X] = true;
                } else if (keyCode == Keycode.Button4) {
                    pressedButtons[(int)Key.C] = true;
                }

                return true;
            }

            return base.OnKeyDown(keyCode, e);
        }

        public override bool OnKeyUp(Keycode keyCode, KeyEvent e)
        {
            if (keyCode == Keycode.Back) {
                pressedButtons[(int)Key.Escape] = false;
                return true;
            }

            pressedButtons[(int)ToDuality(keyCode)] = false;

            // ToDo: Remove this... gamepad to keyboard redirection
            if (keyCode == Keycode.Button1) {
                pressedButtons[(int)Key.ShiftLeft] = false;
            } else if (keyCode == Keycode.Button2) {
                pressedButtons[(int)Key.ControlLeft] = false;
            } else if (keyCode == Keycode.Button3) {
                pressedButtons[(int)Key.X] = false;
            } else if (keyCode == Keycode.Button4) {
                pressedButtons[(int)Key.C] = false;
            }

            //return base.OnKeyUp(keyCode, e);
            return true;
        }

        private Key ToDuality(Keycode key)
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

                case Keycode.DpadUp: return Key.Up;
                case Keycode.DpadDown: return Key.Down;
                case Keycode.DpadLeft: return Key.Left;
                case Keycode.DpadRight: return Key.Right;

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

            if (key >= Keycode.A && key <= Keycode.Z)
                return key - Keycode.A + Key.A;
            if (key >= Keycode.F1 && key <= Keycode.F12)
                return key - Keycode.F1 + Key.F1;
            if (key >= Keycode.Numpad0 && key <= Keycode.NumpadEnter)
                return key - Keycode.Numpad0 + Key.Keypad0;
            if (key >= Keycode.Num0 && key <= Keycode.Num9)
                return key - Keycode.Num0 + Key.Number0;

            return Key.Unknown;
        }

        private class KeyboardInputSource : IKeyboardInputSource
        {
            private readonly InnerView owner;

            public bool this[Key key] => owner.pressedButtons[(int)key];

            public string CharInput => "";

            string IUserInputSource.Id => "Android Keyboard Provider";
            string IUserInputSource.ProductName => "Android Keyboard Provider";
            Guid IUserInputSource.ProductId => Guid.Empty;

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

        // ToDo: Implement this properly
        /*private class GamepadInputSource : IGamepadInputSource
        {
            private readonly InnerView owner;

            public bool this[GamepadButton button] => false;

            public float this[GamepadAxis axis] => 0;

            public string Description => "Android Gamepad Provider";

            public bool IsAvailable => true;

            public GamepadInputSource(InnerView owner)
            {
                this.owner = owner;
            }

            public void SetVibration(float left, float right)
            {
                float average = (left + right) * 0.5f;
                owner.vibrator.Vibrate((long)(9 + average * 4));
            }

            public void UpdateState()
            {
                // Nothing to do...
            }
        }*/
    }
}