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
        public struct VirtualButton
        {
            public PlayerActions Action;
            
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
#endif

        private bool[] pressedButtons = new bool[(int)Key.Last + 1];

        private void InitializeInput()
        {
#if ENABLE_TOUCH
            if (virtualButtons != null) {
                // It's already initialized...
                return;
            }

            const float dpadLeft = 0.02f;
            const float dpadTop = 0.58f;
            const float dpadWidth = 0.2f;
            const float dpadHeight = 0.37f;
            const float dpadThresholdX = 0.05f;
            const float dpadThresholdY = 0.09f;

            IImageCodec imageCodec = ImageCodec.GetRead(ImageCodec.FormatPng);
            AssetManager assets = Context.Assets;

            Material matDpad = LoadButtonImageFromAssets(assets, imageCodec, "dpad.png");
            Material matFire = LoadButtonImageFromAssets(assets, imageCodec, "fire.png");
            Material matJump = LoadButtonImageFromAssets(assets, imageCodec, "jump.png");
            Material matRun = LoadButtonImageFromAssets(assets, imageCodec, "run.png");
            Material matSwitchWeapon = LoadButtonImageFromAssets(assets, imageCodec, "switch.png");

            virtualButtons = new[] {
                new VirtualButton { Action = PlayerActions.None, Left = dpadLeft, Top = dpadTop, Width = dpadWidth, Height = dpadHeight, Material = matDpad, CurrentPointerId = -1 },

                new VirtualButton { Action = PlayerActions.Left, Left = dpadLeft - dpadThresholdX, Top = dpadTop, Width = (dpadWidth / 3) + dpadThresholdX, Height = dpadHeight, CurrentPointerId = -1 },
                new VirtualButton { Action = PlayerActions.Right, Left = (dpadLeft + (dpadWidth * 2 / 3)), Top = dpadTop, Width = (dpadWidth / 3) + dpadThresholdX, Height = dpadHeight, CurrentPointerId = -1 },
                new VirtualButton { Action = PlayerActions.Up, Left = dpadLeft, Top = dpadTop - dpadThresholdY, Width = dpadWidth, Height = (dpadHeight / 3) + dpadThresholdY, CurrentPointerId = -1 },
                new VirtualButton { Action = PlayerActions.Down, Left = dpadLeft, Top = (dpadTop + (dpadHeight * 2 / 3)), Width = dpadWidth, Height = (dpadHeight / 3) + dpadThresholdY, CurrentPointerId = -1 },

                new VirtualButton { Action = PlayerActions.Fire, Left = 0.68f, Top = 0.79f, Width = 0.094f, Height = 0.168f, Material = matFire, CurrentPointerId = -1 },
                new VirtualButton { Action = PlayerActions.Jump, Left = 0.785f, Top = 0.71f, Width = 0.094f, Height = 0.168f, Material = matJump, CurrentPointerId = -1 },
                new VirtualButton { Action = PlayerActions.Run, Left = 0.89f, Top = 0.64f, Width = 0.094f, Height = 0.168f, Material = matRun, CurrentPointerId = -1 },
                new VirtualButton { Action = PlayerActions.SwitchWeapon, Left = 0.83f, Top = 0.57f, Width = 0.055f, Height = 0.096f, Material = matSwitchWeapon, CurrentPointerId = -1 }
            };

            showVirtualButtons = true;
            allowVibrations = Preferences.Get("Vibrations", true);
#endif

            DualityApp.Keyboard.Source = new KeyboardInputSource(this);
        }

        public override bool OnTouchEvent(MotionEvent e)
        {
#if ENABLE_TOUCH
            if (virtualButtons != null) {
                showVirtualButtons = true;

                MotionEventActions action = e.ActionMasked;
                if (action == MotionEventActions.Down || action == MotionEventActions.PointerDown) {
                    int pointerIndex = e.ActionIndex;
                    float x = e.GetX(pointerIndex);
                    float y = e.GetY(pointerIndex);

                    bool vibrated = false;
                    for (int i = 0; i < virtualButtons.Length; i++) {
                        ref VirtualButton button = ref virtualButtons[i];
                        if (button.Action != PlayerActions.None) {
                            if (button.CurrentPointerId == -1 && IsOnButton(ref button, x, y)) {
                                int pointerId = e.GetPointerId(pointerIndex);

                                button.CurrentPointerId = pointerId;
                                ControlScheme.InternalTouchAction(button.Action, true);

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
                        if (button.Action != PlayerActions.None) {
                            if (button.CurrentPointerId != -1) {
                                int pointerIndex = e.FindPointerIndex(button.CurrentPointerId);

                                if (!IsOnButton(ref button, e.GetX(pointerIndex), e.GetY(pointerIndex))) {
                                    button.CurrentPointerId = -1;
                                    ControlScheme.InternalTouchAction(button.Action, false);
                                }
                            } else {
                                for (int j = 0; j < pointerCount; j++) {
                                    if (IsOnButton(ref button, e.GetX(j), e.GetY(j))) {
                                        int pointerId = e.GetPointerId(j);

                                        button.CurrentPointerId = pointerId;
                                        ControlScheme.InternalTouchAction(button.Action, true);

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
                            ControlScheme.InternalTouchAction(button.Action, false);
                        }
                    }

                } else if (action == MotionEventActions.PointerUp) {
                    int pointerId = e.GetPointerId(e.ActionIndex);

                    for (int i = 0; i < virtualButtons.Length; i++) {
                        ref VirtualButton button = ref virtualButtons[i];
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
        private bool IsOnButton(ref VirtualButton button, float x, float y)
        {
            float left = button.Left * viewportWidth;
            if (x < left) return false;

            float top = button.Top * viewportHeight;
            if (y < top) return false;

            float right = left + button.Width * viewportWidth;
            if (x > right) return false;

            float bottom = top + button.Height * viewportHeight;
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
                ControlScheme.InternalTouchAction(PlayerActions.Menu, false);
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

            return true;
        }
        
#if ENABLE_TOUCH
        private static Material LoadButtonImageFromAssets(AssetManager assets, IImageCodec imageCodec, string filename)
        {
            using (Stream s = assets.Open(filename)) {
                return new Material(DrawTechnique.Alpha, new Texture(new Pixmap(imageCodec.Read(s)), TextureSizeMode.NonPowerOfTwo));
            }
        }
#endif

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
    }
}