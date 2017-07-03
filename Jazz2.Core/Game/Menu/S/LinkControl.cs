using Duality;
using Duality.Drawing;
using Duality.Input;
using System;

namespace Jazz2.Game.Menu.S
{
    public class LinkControl : MenuControlBase
    {
        private string title;
        private Action action;

        public override bool IsEnabled => true;

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

                api.DrawStringShadow(device, ref charOffset, title, pos.X, pos.Y,
                    Alignment.Center, null, size, 0.7f, 1.1f, 1.1f, charSpacing: 0.9f);
            } else {
                api.DrawString(device, ref charOffset, title, pos.X, pos.Y, Alignment.Center,
                    ColorRgba.TransparentBlack, 0.9f);
            }

            pos.Y += 30f;
        }

        public override void OnUpdate()
        {
            if (DualityApp.Keyboard.KeyHit(Key.Enter)) {
                action();
            }
        }
    }
}