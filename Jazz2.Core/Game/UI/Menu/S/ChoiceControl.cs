using Duality;
using Duality.Drawing;
using Duality.Input;

namespace Jazz2.Game.UI.Menu.S
{
    public class ChoiceControl : MenuControlBase
    {
        private string title;
        private string[] choices;
        private int selectedIndex;
        private bool allowWrap;
        private bool enabled = true;

        public override bool IsEnabled
        {
            get
            {
                return enabled;
            }
            set
            {
                enabled = value;
            }
        }

        public override bool IsInputCaptured => false;

        public int SelectedIndex => selectedIndex;

        public ChoiceControl(MainMenu api, string title, int selectedIndex, params string[] choices) : base(api)
        {
            this.title = title;
            this.choices = choices;
            this.selectedIndex = selectedIndex;
        }

        public override void OnDraw(IDrawDevice device, Canvas c, ref Vector2 pos, bool focused)
        {
            int charOffset = 0;

            if (focused) {
                float size = 0.5f + /*MainMenu.EaseOutElastic(animation) **/ 0.6f;

                api.DrawMaterial(c, "MenuGlow", pos.X, pos.Y, Alignment.Center, ColorRgba.White.WithAlpha(0.4f * size), (title.Length + 3) * 0.5f * size, 4f * size);

                api.DrawStringShadow(device, ref charOffset, title, pos.X, pos.Y,
                    Alignment.Center, null, size, 0.7f, 1.1f, 1.1f, charSpacing: 0.9f);
            } else if (!enabled) {
                api.DrawString(device, ref charOffset, title, pos.X, pos.Y, Alignment.Center,
                    new ColorRgba(0.4f, 0.3f), 0.9f);
            } else {
                api.DrawString(device, ref charOffset, title, pos.X, pos.Y, Alignment.Center,
                    ColorRgba.TransparentBlack, 0.9f);
            }

            if (choices.Length == 0) {
                return;
            }

            float offset, spacing;
            if (choices.Length == 1) {
                offset = spacing = 0f;
            } else if (choices.Length == 2) {
                offset = 50f;
                spacing = 100f;
            } else {
                offset = 100f;
                spacing = 300f / choices.Length;
            }

            for (int i = 0; i < choices.Length; i++) {
                float x = pos.X - offset + i * spacing;
                if (selectedIndex == i) {
                    api.DrawMaterial(c, "MenuGlow", x, pos.Y + 28f, Alignment.Center, ColorRgba.White.WithAlpha(0.2f), (choices[i].Length + 3) * 0.4f, 2.2f);

                    api.DrawStringShadow(device, ref charOffset, choices[i], x, pos.Y + 28f, Alignment.Center,
                        null, 0.9f, 0.4f, 0.55f, 0.55f, 8f, 0.9f);
                } else {
                    api.DrawString(device, ref charOffset, choices[i], x, pos.Y + 28f, Alignment.Center,
                        ColorRgba.TransparentBlack, 0.8f, charSpacing: 0.9f);
                }
            }

            api.DrawStringShadow(device, ref charOffset, "<", pos.X - (100f + 40f), pos.Y + 28f, Alignment.Center,
                ColorRgba.TransparentBlack, 0.7f);
            api.DrawStringShadow(device, ref charOffset, ">", pos.X + (100f + 40f), pos.Y + 28f, Alignment.Center,
                ColorRgba.TransparentBlack, 0.7f);

            pos.Y += 70f;
        }

        public override void OnUpdate()
        {
            if (ControlScheme.MenuActionHit(PlayerActions.Left)) {
                if (selectedIndex > 0) {
                    selectedIndex--;
                } else if (allowWrap) {
                    selectedIndex = choices.Length - 1;
                }
            } else if (ControlScheme.MenuActionHit(PlayerActions.Right)) {
                if (selectedIndex < choices.Length - 1) {
                    selectedIndex++;
                } else if (allowWrap) {
                    selectedIndex = 0;
                }
            }
        }
    }
}