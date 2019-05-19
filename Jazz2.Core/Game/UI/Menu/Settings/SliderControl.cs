using Duality;
using Duality.Drawing;

namespace Jazz2.Game.UI.Menu.Settings
{
    public class SliderControl : MenuControlBase
    {
        private string title;
        private float currentValue, minValue, maxValue;
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

        public float CurrentValue => currentValue;

        public SliderControl(IMenuContainer api, string title, float currentValue, float minValue, float maxValue) : base(api)
        {
            this.title = title;
            this.currentValue = currentValue;
            this.minValue = minValue;
            this.maxValue = maxValue;
        }

        public override void OnDraw(Canvas canvas, ref Vector2 pos, bool focused, float animation)
        {
            int charOffset = 0;
            int charOffset2 = 0;

            api.DrawStringShadow(ref charOffset, new string('1', 22), pos.X - 47f, pos.Y + 22f, Alignment.Left,
                new ColorRgba(0.38f, 0.37f, 0.34f, 0.36f), 1f, charSpacing: 0.44f);

            string slider = new string('1', (int)((currentValue - minValue) / (maxValue - minValue) * 22));

            if (focused) {
                float size = 0.5f + Ease.OutElastic(animation) * 0.6f;

                api.DrawMaterial("MenuGlow", pos.X, pos.Y, Alignment.Center, ColorRgba.White.WithAlpha(0.4f * size), (title.Length + 3) * 0.5f * size, 4f * size);

                api.DrawString(ref charOffset2, slider, pos.X - 47f, pos.Y + 22f, Alignment.Left,
                    null, 1f, charSpacing: 0.44f);

                api.DrawStringShadow(ref charOffset, title, pos.X, pos.Y,
                    Alignment.Center, null, size, 0.7f, 1.1f, 1.1f, charSpacing: 0.9f);
            } else if (!enabled) {
                api.DrawString(ref charOffset, title, pos.X, pos.Y, Alignment.Center,
                    new ColorRgba(0.4f, 0.3f), 0.9f);
            } else {
                api.DrawString(ref charOffset, title, pos.X, pos.Y, Alignment.Center,
                    ColorRgba.TransparentBlack, 0.9f);

                api.DrawString(ref charOffset2, slider, pos.X - 47f, pos.Y + 22f, Alignment.Left,
                    ColorRgba.TransparentBlack, 1f, charSpacing: 0.44f);
            }

            api.DrawStringShadow(ref charOffset, "<", pos.X - 110f, pos.Y + 20f, Alignment.Center,
                ColorRgba.TransparentBlack, 0.7f);
            api.DrawStringShadow(ref charOffset, ">", pos.X + 110f, pos.Y + 20f, Alignment.Center,
                ColorRgba.TransparentBlack, 0.7f);

            pos.Y += 55f;
        }

        public override void OnUpdate()
        {
            if (ControlScheme.MenuActionHit(PlayerActions.Left)) {
                float diff = (maxValue - minValue) * 0.05f;
                currentValue = MathF.Max(currentValue - diff, minValue);
                api.PlaySound("MenuSelect", 0.3f);
            } else if (ControlScheme.MenuActionHit(PlayerActions.Right)) {
                float diff = (maxValue - minValue) * 0.05f;
                currentValue = MathF.Min(currentValue + diff, maxValue);
                api.PlaySound("MenuSelect", 0.3f);
            }
        }
    }
}