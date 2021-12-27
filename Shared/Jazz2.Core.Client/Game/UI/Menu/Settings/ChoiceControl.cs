﻿using Duality;
using Duality.Drawing;
using Duality.Input;

namespace Jazz2.Game.UI.Menu.Settings
{
    public class ChoiceControl : MenuControlBase
    {
        private string title;
        private string[] choices;
        private int selectedIndex;
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

        public ChoiceControl(IMenuContainer api, string title, int selectedIndex, params string[] choices) : base(api)
        {
            this.title = title;
            this.choices = choices;

            if (selectedIndex >= choices.Length) {
                selectedIndex = 0;
            }

            this.selectedIndex = selectedIndex;
        }

        public override void OnDraw(Canvas canvas, ref Vector2 pos, bool focused, float animation)
        {
            int charOffset = 0;

            if (focused) {
                float size = 0.5f + Ease.OutElastic(animation) * 0.6f;

                api.DrawMaterial("MenuGlow", pos.X, pos.Y, Alignment.Center, ColorRgba.White.WithAlpha(0.4f * size), (title.Length + 3) * 0.5f * size, 4f * size);

                api.DrawStringShadow(ref charOffset, title, pos.X, pos.Y,
                    Alignment.Center, null, size, 0.7f, 1.1f, 1.1f, charSpacing: 0.9f);
            } else if (!enabled) {
                api.DrawString(ref charOffset, title, pos.X, pos.Y, Alignment.Center,
                    new ColorRgba(0.4f, 0.3f), 0.9f);
            } else {
                api.DrawString(ref charOffset, title, pos.X, pos.Y, Alignment.Center,
                    ColorRgba.TransparentBlack, 0.9f);
            }

            if (choices.Length == 0) {
                return;
            }

            api.DrawMaterial("MenuGlow", pos.X, pos.Y + 20f, Alignment.Center, ColorRgba.White.WithAlpha(0.2f), (choices[selectedIndex].Length + 3) * 0.4f, 2.2f);

            api.DrawStringShadow(ref charOffset, choices[selectedIndex], pos.X, pos.Y + 20f, Alignment.Center,
                null, 0.9f, 0.4f, 0.55f, 0.55f, 8f, 0.9f);

            if (!enabled) {
                api.DrawString(ref charOffset, "<", pos.X - 80f, pos.Y + 20f, Alignment.Center,
                    new ColorRgba(0.4f, 0.3f), 0.7f);
                api.DrawString(ref charOffset, ">", pos.X + 80f, pos.Y + 20f, Alignment.Center,
                    new ColorRgba(0.4f, 0.3f), 0.7f);
            } else {
                api.DrawStringShadow(ref charOffset, "<", pos.X - 80f, pos.Y + 20f, Alignment.Center,
                    ColorRgba.TransparentBlack, 0.7f);
                api.DrawStringShadow(ref charOffset, ">", pos.X + 80f, pos.Y + 20f, Alignment.Center,
                    ColorRgba.TransparentBlack, 0.7f);
            }

            pos.Y += 55f;
        }

        public override void OnUpdate()
        {
            if (!enabled) {
                return;
            }

            if (ControlScheme.MenuActionHit(PlayerActions.Left)) {
                if (selectedIndex > 0) {
                    selectedIndex--;
                } else {
                    selectedIndex = choices.Length - 1;
                }
                api.PlaySound("MenuSelect", 0.3f);
            } else if (ControlScheme.MenuActionHit(PlayerActions.Right)) {
                if (selectedIndex < choices.Length - 1) {
                    selectedIndex++;
                } else {
                    selectedIndex = 0;
                }
                api.PlaySound("MenuSelect", 0.3f);
            }
        }
    }
}