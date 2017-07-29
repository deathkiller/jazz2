using Duality;
using Duality.Drawing;
using Duality.Input;
using System;

namespace Jazz2.Game.UI.Menu.S
{
    public class LinkControl : MenuControlBase
    {
        private string title;
        private Action action;
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

        public LinkControl(MainMenu api, string title, Action action) : base(api)
        {
            this.title = title;
            this.action = action;
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

            pos.Y += 40f;
        }

        public override void OnUpdate()
        {
            if (DualityApp.Keyboard.KeyHit(Key.Enter)) {
                action();
            }
        }
    }
}