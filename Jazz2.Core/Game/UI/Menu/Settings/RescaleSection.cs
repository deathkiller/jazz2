using System;
using Duality;
using Duality.Drawing;
using Jazz2.Storage;
using static Jazz2.SettingsCache;

namespace Jazz2.Game.UI.Menu.Settings
{
    public class RescaleSection : MenuSection
    {
        private ResizeMode[] availableModes;
        private string[] availableModesNames;
        private int selectedIndex;
        private float animation;

        public override void OnShow(IMenuContainer root)
        {
            base.OnShow(root);

#if __ANDROID__
            availableModes = new[] {
                ResizeMode.None,
                ResizeMode.HQ2x,
                ResizeMode.GB,
            };
            availableModesNames = new[] {
                "None / Pixel-perfect",
                "HQ2x",
                "GB (limited palette)"
            };
#else
            availableModes = new[] {
                ResizeMode.None,
                ResizeMode.HQ2x,
                ResizeMode.xBRZ3,
                ResizeMode.xBRZ4,
                ResizeMode.GB,
                ResizeMode.CRT
            };
            availableModesNames = new[] {
                "menu/settings/rescale/none".T(),
                "HQ2x",
                "3xBRZ",
                "4xBRZ",
                "GB (limited palette)",
                "CRT"
            };
#endif

            for (int i = 0; i < availableModes.Length; i++) {
                if (availableModes[i] == Resize) {
                    selectedIndex = i;
                    break;
                }
            }
        }

        public override void OnPaint(Canvas canvas, Rect view)
        {
            IDrawDevice device = canvas.DrawDevice;

            Vector2 center = device.TargetSize * 0.5f;

            const float topLine = 131f;
            float bottomLine = device.TargetSize.Y - 42;
            api.DrawMaterial("MenuDim", center.X, (topLine + bottomLine) * 0.5f, Alignment.Center, ColorRgba.White, 55f, (bottomLine - topLine) * 0.063f, new Rect(0f, 0.3f, 1f, 0.4f));

            api.DrawMaterial("MenuLine", 0, center.X, topLine, Alignment.Center, ColorRgba.White, 1.6f);
            api.DrawMaterial("MenuLine", 1, center.X, bottomLine, Alignment.Center, ColorRgba.White, 1.6f);

            int charOffset = 0;
            api.DrawStringShadow(ref charOffset, "menu/settings/rescale/title".T(), center.X, 110f,
                Alignment.Center, new ColorRgba(0.5f, 0.5f), 0.9f, 0.4f, 0.6f, 0.6f, 8f, charSpacing: 0.88f);

            if (availableModes.Length > 0) {
                float topItem = topLine - 5f;
                float bottomItem = bottomLine + 5f;
                float contentHeight = bottomItem - topItem;
                float itemSpacing = contentHeight / (availableModes.Length + 1);

                topItem += itemSpacing;

                for (int i = 0; i < availableModes.Length; i++) {
                    string name = availableModesNames[i];

                    if (selectedIndex == i) {
                        float size = 0.5f + Ease.OutElastic(animation) * 0.6f;

                        api.DrawMaterial("MenuGlow", center.X, topItem, Alignment.Center, ColorRgba.White.WithAlpha(0.4f * size), (name.Length + 3) * 0.5f * size, 4f * size);

                        api.DrawStringShadow(ref charOffset, name, center.X, topItem,
                            Alignment.Center, null, size, 0.7f, 1.1f, 1.1f, charSpacing: 0.9f);
                    } else {
                        api.DrawString(ref charOffset, name, center.X, topItem,
                            Alignment.Center, ColorRgba.TransparentBlack, 0.9f);
                    }

                    topItem += itemSpacing;
                }
            }
        }

        public override void OnUpdate()
        {
            if (animation < 1f) {
                animation = Math.Min(animation + Time.TimeMult * 0.016f, 1f);
            }

            if (ControlScheme.MenuActionHit(PlayerActions.Fire)) {
                api.PlaySound("MenuSelect", 0.5f);
                ApplyRescaleMode(availableModes[selectedIndex]);
            } else if (ControlScheme.MenuActionHit(PlayerActions.Menu)) {
                api.PlaySound("MenuSelect", 0.5f);
                api.LeaveSection(this);
            } else if (ControlScheme.MenuActionHit(PlayerActions.Up)) {
                api.PlaySound("MenuSelect", 0.4f);
                animation = 0f;
                if (selectedIndex > 0) {
                    selectedIndex--;
                } else {
                    selectedIndex = availableModes.Length - 1;
                }
            } else if (ControlScheme.MenuActionHit(PlayerActions.Down)) {
                api.PlaySound("MenuSelect", 0.4f);
                animation = 0f;
                if (selectedIndex < availableModes.Length - 1) {
                    selectedIndex++;
                } else {
                    selectedIndex = 0;
                }
            }
        }

        private void ApplyRescaleMode(ResizeMode mode)
        {
            Resize = mode;

            Preferences.Set("Resize", (byte)Resize);
            Preferences.Commit();

            api.LeaveSection(this);
        }
    }
}