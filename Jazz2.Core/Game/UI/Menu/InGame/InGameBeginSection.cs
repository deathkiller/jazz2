using System;
using System.Collections.Generic;
using Duality;
using Duality.Drawing;
using Duality.Input;
using Jazz2.Game.UI.Menu.Settings;

namespace Jazz2.Game.UI.Menu.InGame
{
    public class InGameMenuBeginSection : MenuSection
    {
        private List<Tuple<string, Action>> items;

        private int selectedIndex;
        private float animation;

        public InGameMenuBeginSection(bool isMultiplayerSession)
        {
            items = new List<Tuple<string, Action>> {
                Tuple.Create<string, Action>("Resume", OnPlayStoryPressed),
                Tuple.Create<string, Action>("Settings", OnSettingsPressed),
                Tuple.Create<string, Action>(isMultiplayerSession ? "Disconnect & Exit" : "Save & Exit", OnExitPressed),
            };
        }

        public override void OnShow(IMenuContainer root)
        {
            animation = 0f;
            base.OnShow(root);

            api.PlaySound("MenuSelect", 0.4f);
        }

        public override void OnPaint(Canvas canvas, Rect view)
        {
            IDrawDevice device = canvas.DrawDevice;

            Vector2 center = device.TargetSize * 0.5f;
            center.Y *= 0.96f;

            const float topLine = 131f;
            float bottomLine = device.TargetSize.Y - 42;
            api.DrawMaterial("MenuDim", center.X, (topLine + bottomLine) * 0.5f, Alignment.Center, new ColorRgba(0f, 1f), 80f, (bottomLine - topLine) / 7.6f);

            int charOffset = 0;
            for (int i = 0; i < items.Count; i++) {
                if (selectedIndex == i) {
                    float size = 0.5f + Ease.OutElastic(animation) * 0.6f;

                    api.DrawMaterial("MenuGlow", center.X, center.Y, Alignment.Center, ColorRgba.White.WithAlpha(0.2f * size), (items[i].Item1.Length + 3) * 0.5f * size, 4f * size);

                    api.DrawStringShadow(ref charOffset, items[i].Item1, center.X, center.Y,
                        Alignment.Center, null, size, 0.7f, 1.1f, 1.1f, charSpacing: 0.9f);
                } else {
                    api.DrawString(ref charOffset, items[i].Item1, center.X, center.Y,
                        Alignment.Center, new ColorRgba(0.4f, 0.5f), 0.9f);
                }

                center.Y += 34f + 8f;
            }
        }

        public override void OnUpdate()
        {
            if (animation < 1f) {
                animation = Math.Min(animation + Time.TimeMult * 0.016f, 1f);
            }

            if (ControlScheme.MenuActionHit(PlayerActions.Fire)) {
                api.PlaySound("MenuSelect", 0.5f);
                items[selectedIndex].Item2();
            } else if (ControlScheme.MenuActionHit(PlayerActions.Menu)) {
                api.PlaySound("MenuSelect", 0.5f);
                ((InGameMenu)api).SwitchToCurrentGame();
            } else if (ControlScheme.MenuActionHit(PlayerActions.Up)) {
                api.PlaySound("MenuSelect", 0.4f);
                animation = 0f;
                if (selectedIndex > 0) {
                    selectedIndex--;
                } else {
                    selectedIndex = items.Count - 1;
                }
            } else if (ControlScheme.MenuActionHit(PlayerActions.Down)) {
                api.PlaySound("MenuSelect", 0.4f);
                animation = 0f;
                if (selectedIndex < items.Count - 1) {
                    selectedIndex++;
                } else {
                    selectedIndex = 0;
                }
            }
        }

        private void OnPlayStoryPressed()
        {
            ((InGameMenu)api).SwitchToCurrentGame();
        }

        private void OnSettingsPressed()
        {
            api.SwitchToSection(new SettingsSection());
        }

        private void OnExitPressed()
        {
            ((InGameMenu)api).SwitchToMainMenu();
        }
    }
}