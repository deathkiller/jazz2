using Duality;
using Duality.Drawing;

namespace Jazz2.Game.UI.Menu
{
    public class SimpleMessageSection : MenuSection
    {
        private string title;
        private string message;

        public SimpleMessageSection(string title, string message)
        {
            this.title = title;
            this.message = message;
        }

        public override void OnPaint(Canvas canvas, Rect view)
        {
            IDrawDevice device = canvas.DrawDevice;

            Vector2 center = device.TargetSize * 0.5f;

            int charOffset = 0;
            api.DrawStringShadow(ref charOffset, title, center.X, center.Y - 50, Alignment.Center,
                    new ColorRgba(0.62f, 0.44f, 0.34f, 0.5f), 1.1f, 0.4f, 0.6f, 0.6f, 8f, charSpacing: 0.86f);

            api.DrawStringShadow(ref charOffset, message, center.X, center.Y + 20, Alignment.Center,
                    ColorRgba.TransparentBlack, 0.9f, 0.4f, 0.6f, 0.6f, 8f, charSpacing: 0.88f);

            charOffset = 2;

            // TODO: translation
            const string BackText = "Back";
            api.DrawMaterial("MenuGlow", center.X, center.Y + 110, Alignment.Center,
                ColorRgba.White.WithAlpha(0.4f * /*size*/1.1f), (BackText.Length + 3) * 0.5f * /*size*/1.1f, 4f * /*size*/1.1f);

            api.DrawStringShadow(ref charOffset, BackText, center.X, center.Y + 110,
                Alignment.Center, null, /*size*/1.1f, 0.7f, 1.1f, 1.1f, charSpacing: 0.9f);
        }

        public override void OnUpdate()
        {
            if (ControlScheme.MenuActionHit(PlayerActions.Fire) ||
                ControlScheme.MenuActionHit(PlayerActions.Menu)) {
                api.PlaySound("MenuSelect", 0.5f);
                api.LeaveSection(this);
            }
        }
    }
}