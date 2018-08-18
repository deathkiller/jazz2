using System;
using Duality;
using Duality.Drawing;
using Duality.Input;
using static Jazz2.ControlScheme;

namespace Jazz2.Game.UI.Menu.Settings
{
    public class ControlsSection : MainMenuSection
    {
        private const int possibleButtons = 3;

        private int selectedIndex, selectedColumn;
        private float animation;
        private bool waitForInput;

        public ControlsSection()
        {

        }

        public override void OnShow(MainMenu root)
        {
            animation = 0f;
            base.OnShow(root);
        }

        public override void OnPaint(Canvas canvas)
        {
            IDrawDevice device = canvas.DrawDevice;

            Vector2 center = device.TargetSize * 0.5f;

            const float topLine = 131f;
            float bottomLine = device.TargetSize.Y - 42;
            api.DrawMaterial("MenuDim", center.X, (topLine + bottomLine) * 0.5f, Alignment.Center, ColorRgba.White, 55f, (bottomLine - topLine) * 0.063f, new Rect(0f, 0.3f, 1f, 0.4f));

            int charOffset = 0;
            api.DrawStringShadow(ref charOffset, "Controls for Player #1", center.X * 0.3f, 110f,
                Alignment.Left, new ColorRgba(0.5f, 0.5f), 0.9f, 0.4f, 0.6f, 0.6f, 8f, charSpacing: 0.88f);

            api.DrawStringShadow(ref charOffset, "Key 1", center.X * (0.9f + 0 * 0.34f), 110f,
                Alignment.Center, new ColorRgba(0.46f, 0.5f), 0.8f, charSpacing: 0.88f);
            api.DrawStringShadow(ref charOffset, "Key 2", center.X * (0.9f + 1 * 0.34f), 110f,
                Alignment.Center, new ColorRgba(0.46f, 0.5f), 0.8f, charSpacing: 0.88f);
            api.DrawStringShadow(ref charOffset, "Gamepad", center.X * (0.9f + 2 * 0.34f), 110f,
                Alignment.Center, new ColorRgba(0.46f, 0.5f), 0.8f, charSpacing: 0.88f);

            int n = (int)PlayerActions.Count;

            float topItem = topLine - 5f;
            float bottomItem = bottomLine + 5f;
            float contentHeight = bottomItem - topItem;
            float itemSpacing = contentHeight / (n + 1);

            topItem += itemSpacing;

            for (int i = 0; i < n; i++) {
                string name;
                switch ((PlayerActions)i) {
                    case PlayerActions.Left: name = "Left"; break;
                    case PlayerActions.Right: name = "Right"; break;
                    case PlayerActions.Up: name = "Up / Look Up"; break;
                    case PlayerActions.Down: name = "Down / Crouch"; break;
                    case PlayerActions.Fire: name = "Fire"; break;
                    case PlayerActions.Jump: name = "Jump"; break;
                    case PlayerActions.Run: name = "Run"; break;
                    case PlayerActions.SwitchWeapon: name = "Switch Weapon"; break;
                    default: name = ((PlayerActions)i).ToString(); break;
                }

                ref Mapping mapping = ref ControlScheme.GetCurrentMapping(0, (PlayerActions)i);

                api.DrawString(ref charOffset, name, center.X * 0.3f, topItem,
                    Alignment.Left, ColorRgba.TransparentBlack, 0.8f);

                for (int j = 0; j < possibleButtons; j++) {
                    string value;
                    switch (j) {
                        case 0:
                            if (mapping.Key1 != Key.Unknown) {
                                value = mapping.Key1.ToString();
                            } else {
                                value = "-";
                            }
                            break;
                        case 1:
                            if (mapping.Key2 != Key.Unknown) {
                                value = mapping.Key2.ToString();
                            } else {
                                value = "-";
                            }
                            break;
                        case 2:
                            if (mapping.GamepadIndex != -1) {
                                value = mapping.GamepadIndex + " : " + mapping.GamepadButton;
                            } else {
                                value = "-";
                            }
                            break;

                        default: value = null; break;
                    }

                    if (selectedIndex == i && selectedColumn == j) {
                        float size = 0.5f + Ease.OutElastic(animation) * 0.5f;

                        api.DrawStringShadow(ref charOffset, value, center.X * (0.9f + j * 0.34f), topItem,
                            Alignment.Center, waitForInput ? new ColorRgba(0.62f, 0.44f, 0.34f, 0.5f) : new ColorRgba(0.48f, 0.5f), size, 0.7f, 1.1f, 1.1f, charSpacing: 0.9f);

                    } else {
                        api.DrawString(ref charOffset, value, center.X * (0.9f + j * 0.34f), topItem,
                            Alignment.Center, ColorRgba.TransparentBlack, 0.8f);
                    }
                }

                topItem += itemSpacing;
            }


            api.DrawMaterial("MenuLine", 0, center.X, topLine, Alignment.Center, ColorRgba.White, 1.6f);
            api.DrawMaterial("MenuLine", 1, center.X, bottomLine, Alignment.Center, ColorRgba.White, 1.6f);
        }

        public override void OnUpdate()
        {
            if (animation < 1f) {
                animation = Math.Min(animation + Time.TimeMult * 0.016f, 1f);
            }

            if (waitForInput) {
                if (DualityApp.Keyboard.KeyHit(Key.Escape)) {
                    api.PlaySound("MenuSelect", 0.5f);
                    waitForInput = false;
                    return;
                }

                switch (selectedColumn) {
                    case 0: // Keyboard
                    case 1:
                        for (Key key = 0; key < Key.Last; key++) {
                            if (DualityApp.Keyboard.KeyHit(key)) {
                                ref Mapping mapping = ref ControlScheme.GetCurrentMapping(0, (PlayerActions)selectedIndex);

                                if (selectedColumn == 0) {
                                    mapping.Key1 = key;
                                } else {
                                    mapping.Key2 = key;
                                }

                                api.PlaySound("MenuSelect", 0.5f);
                                waitForInput = false;
                                break;
                            }
                        }
                        break;

                    case 2: // Gamepad
                        for (int i = 0; i < DualityApp.Gamepads.Count; i++) {
                            for (GamepadButton button = 0; button < GamepadButton.Last; button++) {
                                if (DualityApp.Gamepads[i].ButtonHit(button)) {
                                    ref Mapping mapping = ref ControlScheme.GetCurrentMapping(0, (PlayerActions)selectedIndex);

                                    mapping.GamepadIndex = i;
                                    mapping.GamepadButton = button;

                                    api.PlaySound("MenuSelect", 0.5f);
                                    waitForInput = false;
                                    break;
                                }
                            }
                        }
                        break;
                }
                return;
            }

            if (ControlScheme.MenuActionHit(PlayerActions.Fire)) {
                api.PlaySound("MenuSelect", 0.5f);
                waitForInput = true;
            } else if (DualityApp.Keyboard.KeyHit(Key.Delete)) {
                api.PlaySound("MenuSelect", 0.5f);

                ref Mapping mapping = ref ControlScheme.GetCurrentMapping(0, (PlayerActions)selectedIndex);
                switch (selectedColumn) {
                    case 0: mapping.Key1 = Key.Unknown; break;
                    case 1: mapping.Key2 = Key.Unknown; break;
                    case 2:mapping.GamepadIndex = -1; break;
                }
            } else if (DualityApp.Keyboard.KeyHit(Key.Escape)) {
                api.PlaySound("MenuSelect", 0.5f);
                api.LeaveSection(this);
            }

            if (ControlScheme.MenuActionHit(PlayerActions.Up)) {
                api.PlaySound("MenuSelect", 0.4f);
                animation = 0f;
                if (selectedIndex > 0) {
                    selectedIndex--;
                } else {
                    selectedIndex = (int)PlayerActions.Count - 1;
                }
            } else if (ControlScheme.MenuActionHit(PlayerActions.Down)) {
                api.PlaySound("MenuSelect", 0.4f);
                animation = 0f;
                if (selectedIndex < (int)PlayerActions.Count - 1) {
                    selectedIndex++;
                } else {
                    selectedIndex = 0;
                }
            } else if (ControlScheme.MenuActionHit(PlayerActions.Left)) {
                api.PlaySound("MenuSelect", 0.4f);
                animation = 0f;
                if (selectedColumn > 0) {
                    selectedColumn--;
                } else {
                    selectedColumn = possibleButtons - 1;
                }
            } else if (ControlScheme.MenuActionHit(PlayerActions.Right)) {
                api.PlaySound("MenuSelect", 0.4f);
                animation = 0f;
                if (selectedColumn < possibleButtons - 1) {
                    selectedColumn++;
                } else {
                    selectedColumn = 0;
                }
            }
        }
    }
}