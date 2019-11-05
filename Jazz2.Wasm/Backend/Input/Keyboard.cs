using System;
using Duality.Input;
using WebAssembly;

namespace Duality.Backend.Wasm
{
    public class KeyboardInputSource : IKeyboardInputSource
    {
        private bool[] keyState = new bool[256];

        public KeyboardInputSource()
        {
            using (JSObject document = (JSObject)Runtime.GetGlobalObject("document")) {
                document.Invoke("addEventListener", "keydown", new Action<JSObject>(e => {
                    int keyCode = (int)e.GetObjectProperty("which");
                    e.Invoke("preventDefault");
                    e.Invoke("stopPropagation");
                    e.Dispose();

                    if (keyCode < keyState.Length) {
                        keyState[keyCode] = true;
                    }
                }), false);

                document.Invoke("addEventListener", "keyup", new Action<JSObject>(e => {
                    int keyCode = (int)e.GetObjectProperty("which");
                    e.Invoke("preventDefault");
                    e.Invoke("stopPropagation");
                    e.Dispose();

                    if (keyCode < keyState.Length) {
                        keyState[keyCode] = false;
                    }
                }), false);
            }
        }

        public bool this[Key key] => this.keyState[GetJSKey(key)];

        public string CharInput => "";

        public string Id => "";

        public Guid ProductId => Guid.Empty;

        public string ProductName => "";

        public bool IsAvailable => true;

        public void UpdateState()
        {
        }

        private static int GetJSKey(Key key)
        {
            switch (key) {
                case Key.Unknown: return 0;

                case Key.ShiftLeft: return 16;
                //case Key.ShiftRight: return OpenTK.Input.Key.ShiftRight;
                case Key.ControlLeft: return 17;
                //case Key.ControlRight: return OpenTK.Input.Key.ControlRight;
                case Key.AltLeft: return 18;
                //case Key.AltRight: return OpenTK.Input.Key.AltRight;
                case Key.WinLeft: return 91;
                //case Key.WinRight: return OpenTK.Input.Key.WinRight;
                case Key.Menu: return 93;

                case Key.F1: return 112;
                case Key.F2: return 113;
                case Key.F3: return 114;
                case Key.F4: return 115;
                case Key.F5: return 116;
                case Key.F6: return 117;
                case Key.F7: return 118;
                case Key.F8: return 119;
                case Key.F9: return 120;
                case Key.F10: return 121;
                case Key.F11: return 122;
                case Key.F12: return 123;
                /*case Key.F13: return OpenTK.Input.Key.F13;
                case Key.F14: return OpenTK.Input.Key.F14;
                case Key.F15: return OpenTK.Input.Key.F15;
                case Key.F16: return OpenTK.Input.Key.F16;
                case Key.F17: return OpenTK.Input.Key.F17;
                case Key.F18: return OpenTK.Input.Key.F18;
                case Key.F19: return OpenTK.Input.Key.F19;
                case Key.F20: return OpenTK.Input.Key.F20;
                case Key.F21: return OpenTK.Input.Key.F21;
                case Key.F22: return OpenTK.Input.Key.F22;
                case Key.F23: return OpenTK.Input.Key.F23;
                case Key.F24: return OpenTK.Input.Key.F24;
                case Key.F25: return OpenTK.Input.Key.F25;
                case Key.F26: return OpenTK.Input.Key.F26;
                case Key.F27: return OpenTK.Input.Key.F27;
                case Key.F28: return OpenTK.Input.Key.F28;
                case Key.F29: return OpenTK.Input.Key.F29;
                case Key.F30: return OpenTK.Input.Key.F30;
                case Key.F31: return OpenTK.Input.Key.F31;
                case Key.F32: return OpenTK.Input.Key.F32;
                case Key.F33: return OpenTK.Input.Key.F33;
                case Key.F34: return OpenTK.Input.Key.F34;
                case Key.F35: return OpenTK.Input.Key.F35;*/

                case Key.Up: return 38;
                case Key.Down: return 40;
                case Key.Left: return 37;
                case Key.Right: return 39;

                case Key.Enter: return 13;
                case Key.Escape: return 27;
                case Key.Space: return 32;
                case Key.Tab: return 9;
                case Key.BackSpace: return 8;
                case Key.Insert: return 45;
                case Key.Delete: return 46;
                case Key.PageUp: return 33;
                case Key.PageDown: return 34;
                case Key.Home: return 36;
                case Key.End: return 35;
                /*case Key.CapsLock: return OpenTK.Input.Key.CapsLock;
                case Key.ScrollLock: return OpenTK.Input.Key.ScrollLock;
                case Key.PrintScreen: return OpenTK.Input.Key.PrintScreen;
                case Key.Pause: return OpenTK.Input.Key.Pause;
                case Key.NumLock: return OpenTK.Input.Key.NumLock;
                case Key.Clear: return OpenTK.Input.Key.Clear;
                case Key.Sleep: return OpenTK.Input.Key.Sleep;

                case Key.Keypad0: return OpenTK.Input.Key.Keypad0;
                case Key.Keypad1: return OpenTK.Input.Key.Keypad1;
                case Key.Keypad2: return OpenTK.Input.Key.Keypad2;
                case Key.Keypad3: return OpenTK.Input.Key.Keypad3;
                case Key.Keypad4: return OpenTK.Input.Key.Keypad4;
                case Key.Keypad5: return OpenTK.Input.Key.Keypad5;
                case Key.Keypad6: return OpenTK.Input.Key.Keypad6;
                case Key.Keypad7: return OpenTK.Input.Key.Keypad7;
                case Key.Keypad8: return OpenTK.Input.Key.Keypad8;
                case Key.Keypad9: return OpenTK.Input.Key.Keypad9;
                case Key.KeypadDivide: return OpenTK.Input.Key.KeypadDivide;
                case Key.KeypadMultiply: return OpenTK.Input.Key.KeypadMultiply;
                case Key.KeypadSubtract: return OpenTK.Input.Key.KeypadSubtract;
                case Key.KeypadAdd: return OpenTK.Input.Key.KeypadAdd;
                case Key.KeypadDecimal: return OpenTK.Input.Key.KeypadDecimal;
                case Key.KeypadEnter: return OpenTK.Input.Key.KeypadEnter;*/

                case Key.A: return 65;
                case Key.B: return 66;
                case Key.C: return 67;
                case Key.D: return 68;
                case Key.E: return 69;
                case Key.F: return 70;
                case Key.G: return 71;
                case Key.H: return 72;
                case Key.I: return 73;
                case Key.J: return 74;
                case Key.K: return 75;
                case Key.L: return 76;
                case Key.M: return 77;
                case Key.N: return 78;
                case Key.O: return 79;
                case Key.P: return 80;
                case Key.Q: return 81;
                case Key.R: return 82;
                case Key.S: return 83;
                case Key.T: return 84;
                case Key.U: return 85;
                case Key.V: return 86;
                case Key.W: return 87;
                case Key.X: return 88;
                case Key.Y: return 89;
                case Key.Z: return 90;

                /*case Key.Number0: return OpenTK.Input.Key.Number0;
                case Key.Number1: return OpenTK.Input.Key.Number1;
                case Key.Number2: return OpenTK.Input.Key.Number2;
                case Key.Number3: return OpenTK.Input.Key.Number3;
                case Key.Number4: return OpenTK.Input.Key.Number4;
                case Key.Number5: return OpenTK.Input.Key.Number5;
                case Key.Number6: return OpenTK.Input.Key.Number6;
                case Key.Number7: return OpenTK.Input.Key.Number7;
                case Key.Number8: return OpenTK.Input.Key.Number8;
                case Key.Number9: return OpenTK.Input.Key.Number9;

                case Key.Tilde: return OpenTK.Input.Key.Tilde;
                case Key.Minus: return OpenTK.Input.Key.Minus;
                case Key.Plus: return OpenTK.Input.Key.Plus;
                case Key.BracketLeft: return OpenTK.Input.Key.BracketLeft;
                case Key.BracketRight: return OpenTK.Input.Key.BracketRight;
                case Key.Semicolon: return OpenTK.Input.Key.Semicolon;
                case Key.Quote: return OpenTK.Input.Key.Quote;
                case Key.Comma: return OpenTK.Input.Key.Comma;
                case Key.Period: return OpenTK.Input.Key.Period;
                case Key.Slash: return OpenTK.Input.Key.Slash;
                case Key.BackSlash: return OpenTK.Input.Key.BackSlash;
                case Key.NonUSBackSlash: return OpenTK.Input.Key.NonUSBackSlash;*/

                case Key.Last: return 255;
            }

            return 0;
        }
    }
}
