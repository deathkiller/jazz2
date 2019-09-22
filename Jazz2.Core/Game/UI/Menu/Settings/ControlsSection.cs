using System;
using Duality;
using Duality.Drawing;
using Duality.Input;
using static Jazz2.ControlScheme;

namespace Jazz2.Game.UI.Menu.Settings
{
    public class ControlsSection : MenuSection
    {
        private const int possibleButtons = 3;

        private int selectedIndex, selectedColumn;
        private float animation;
        private bool waitForInput;

        public override void OnShow(IMenuContainer root)
        {
            animation = 0f;
            base.OnShow(root);
        }

        public override void OnHide(bool isRemoved)
        {
            if (isRemoved) {
                Commit();
            }

            base.OnHide(isRemoved);
        }

        public override void OnPaint(Canvas canvas, Rect view)
        {
            IDrawDevice device = canvas.DrawDevice;

            Vector2 center = device.TargetSize * 0.5f;

            const float topLine = 131f;
            float bottomLine = device.TargetSize.Y - 42;
            api.DrawMaterial("MenuDim", center.X, (topLine + bottomLine) * 0.5f, Alignment.Center, ColorRgba.White, 55f, (bottomLine - topLine) * 0.063f, new Rect(0f, 0.3f, 1f, 0.4f));

            int charOffset = 0;
            api.DrawStringShadow(ref charOffset, "menu/settings/controls/title".T("1"), center.X * 0.3f, 110f,
                Alignment.Left, new ColorRgba(0.5f, 0.5f), 0.9f, 0.4f, 0.6f, 0.6f, 8f, charSpacing: 0.88f);

            api.DrawStringShadow(ref charOffset, "menu/settings/controls/key".T("1"), center.X * (0.9f + 0 * 0.34f), 110f,
                Alignment.Center, new ColorRgba(0.46f, 0.5f), 0.8f, charSpacing: 0.88f);
            api.DrawStringShadow(ref charOffset, "menu/settings/controls/key".T("2"), center.X * (0.9f + 1 * 0.34f), 110f,
                Alignment.Center, new ColorRgba(0.46f, 0.5f), 0.8f, charSpacing: 0.88f);
            api.DrawStringShadow(ref charOffset, "menu/settings/controls/gamepad".T(), center.X * (0.9f + 2 * 0.34f), 110f,
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
                    case PlayerActions.Left: name = "menu/settings/controls/left".T(); break;
                    case PlayerActions.Right: name = "menu/settings/controls/right".T(); break;
                    case PlayerActions.Up: name = "menu/settings/controls/up".T(); break;
                    case PlayerActions.Down: name = "menu/settings/controls/down".T(); break;
                    case PlayerActions.Fire: name = "menu/settings/controls/fire".T(); break;
                    case PlayerActions.Jump: name = "menu/settings/controls/jump".T(); break;
                    case PlayerActions.Run: name = "menu/settings/controls/run".T(); break;
                    case PlayerActions.SwitchWeapon: name = "menu/settings/controls/switch weapon".T(); break;
                    case PlayerActions.Menu: name = "menu/settings/controls/back".T(); break;
                    default: name = ((PlayerActions)i).ToString(); break;
                }

                ref Mapping mapping = ref GetCurrentMapping(0, (PlayerActions)i);

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

                        ColorRgba? color;
                        if (waitForInput) {
                            color = new ColorRgba(0.62f, 0.44f, 0.34f, 0.5f);
                        } else {
                            color = null;
                        }

                        api.DrawStringShadow(ref charOffset, value, center.X * (0.9f + j * 0.34f), topItem,
                            Alignment.Center, color, size, 0.7f, 1.1f, 1.1f, charSpacing: 0.9f);

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
                //if (ControlScheme.MenuActionHit(PlayerActions.Menu)) {
                //    api.PlaySound("MenuSelect", 0.5f);
                //    waitForInput = false;
                //    return;
                //}

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
                            for (GamepadButton button = 0; button <= GamepadButton.Last; button++) {
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

            if (ControlScheme.MenuActionHit(PlayerActions.Menu)) {
                api.PlaySound("MenuSelect", 0.5f);
                api.LeaveSection(this);
            } else if (ControlScheme.MenuActionHit(PlayerActions.Fire)) {
                if ((PlayerActions)selectedIndex == PlayerActions.Menu && selectedColumn == 0) {
                    return;
                }

                api.PlaySound("MenuSelect", 0.5f);
                animation = 0f;
                waitForInput = true;
            } else if (DualityApp.Keyboard.KeyHit(Key.Delete)) {
                api.PlaySound("MenuSelect", 0.5f);

                ref Mapping mapping = ref ControlScheme.GetCurrentMapping(0, (PlayerActions)selectedIndex);
                switch (selectedColumn) {
                    case 0: mapping.Key1 = Key.Unknown; break;
                    case 1: mapping.Key2 = Key.Unknown; break;
                    case 2:mapping.GamepadIndex = -1; break;
                }
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

        private void Commit()
        {
            ControlScheme.SaveMappings();
        }
    }
}