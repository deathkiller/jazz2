using System;
using Duality;
using Duality.Drawing;

namespace Jazz2.Game.UI.Menu.Settings
{
    public class LinkControl : MenuControlBase
    {
        private string title;
        private string description;
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

        public LinkControl(IMenuContainer api, string title, Action action, string description = null) : base(api)
        {
            this.title = title;
            this.action = action;
            this.description = description;
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

            if (description != null) {
                api.DrawString(ref charOffset, description, pos.X, pos.Y + 20f, Alignment.Center,
                    new ColorRgba(0.5f, 0.42f, 0.38f, 0.48f), 0.7f);

                pos.Y += 55f;
            } else {
                pos.Y += 40f;
            }
        }

        public override void OnUpdate()
        {
            if (ControlScheme.MenuActionHit(PlayerActions.Fire)) {
                api.PlaySound("MenuSelect", 0.5f);
                action();
            }
        }
    }
}